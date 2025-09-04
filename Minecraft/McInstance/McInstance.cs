using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.Minecraft.McInstance.Resources;
using PCL.Core.Minecraft.McLaunch;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.McInstance;

public class McInstance {
    private JsonObject? _versionJson;
    private McInstanceInfo? _versionInfo;

    private List<Library>? _libraries; // 依赖库列表
    private HashSet<string>? _libraryNameHashCache; // 依赖库哈希缓存
    private AssetIndex? _assetIndex;

    private JsonObject? _versionJsonInJar;

    private McInstanceCardType? _cachedDisplayType;

    /// <summary>
    /// 初始化 Minecraft 实例
    /// 初始化后请一定要先运行 Check() 方法
    /// </summary>
    /// <param name="path"></param>
    public McInstance(string path) {
        Path = (path.Contains(':') ? "" : McFolderManager.PathMcFolder + "versions\\") + path + (path.EndsWith('\\') ? "" : "\\");
    }

    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 应用版本隔离后的 Minecraft 根文件夹路径，以“\”结尾
    /// </summary>
    public string? IsolatedPath => McInstanceLogic.GetIsolatedPathAsync(this);

    /// <summary>
    /// 实例文件夹名称
    /// </summary>
    public string Name => string.IsNullOrEmpty(Path) ? "" : new DirectoryInfo(Path).Name;

    /// <summary>
    /// 显示的描述文本
    /// </summary>
    public string Desc { get; set; } = "该实例未被加载，请向作者反馈此问题";

    /// <summary>
    /// 显示的实例图标
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 是否为收藏的实例
    /// </summary>
    public bool IsFavorited { get; set; }

    #region No Patches Compatibility

    private readonly FrozenDictionary<string, string> _patcherIdNameMapping = new Dictionary<string, string> {
            // { "net.fabricmc:fabric-loader", "fabric" }, 
            { "org.quiltmc:quilt-loader", "quilt" },
            { "com.cleanroommc:cleanroom", "cleanroom" },
            { "com.mumfrey:liteloader", "liteloader" },
            // { "optifine:OptiFine", "optifine" },
            // { "net.minecraftforge:forge", "forge" }
        }
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private void ConvertToPatches() {
        if (IsPatchesFormatJson) {
            return;
        }

        ParseLibraryNamesAsHashSet();

        // Quilt & Cleanroom & LiteLoader
        foreach (var pair in _patcherIdNameMapping) {
            var version = FindPatcherVersionsInHashSet(pair.Key);
            if (version != null) {
                _versionInfo!.Patchers.Add(new PatcherInfo {
                    Id = pair.Value,
                    Version = version
                });
            }
        }

        var hasNeoForge = true;

        // NeoForge
        if (FindPatcherVersionsInHashSet("net.neoforged.fancymodloader") != null) {
            try {
                if (_versionInfo!.McVersionStr.IsNullOrEmpty()) {
                    try {
                        FindArgumentData("--fml.neoForgeVersion", "neoforge");
                    } catch {
                        FindArgumentData("--fml.forgeVersion", "neoforge");
                    }
                }
                FindArgumentData(_versionInfo!.McVersionStr == "1.20.1" ? "--fml.forgeVersion" : "--fml.neoForgeVersion", "neoforge");

            } catch (Exception ex) {
                hasNeoForge = false;
                LogWrapper.Warn(ex, "识别 NeoForge 时出错");
            }
        } else {
            hasNeoForge = false;
        }

        var hasFabric = false;
        if (!hasNeoForge) {
            // Fabric & LegacyFabric
            var fabricVersion = FindPatcherVersionsInHashSet("net.fabricmc:fabric-loader");
            if (fabricVersion != null) {
                if (FindPatcherVersionsInHashSet("net.legacyfabric") != null) {
                    _versionInfo!.Patchers.Add(new PatcherInfo {
                        Id = "legacyfabric",
                        Version = fabricVersion
                    });
                } else {
                    _versionInfo!.Patchers.Add(new PatcherInfo {
                        Id = "fabric",
                        Version = fabricVersion
                    });
                }
                hasFabric = true;
            }
        }

        if (!hasNeoForge & !hasFabric) {
            // Forge
            try {
                FindArgumentData("--fml.forgeVersion", "forge");
            } catch (Exception ex) {
                LogWrapper.Warn(ex, "识别 Forge 时出错");
            }
        }

        // OptiFine
        var optiFineVersion = FindPatcherVersionsInHashSet("optifine:OptiFine");
        if (optiFineVersion != null) {
            var parts = optiFineVersion.Split('_', 2);
            if (parts.Length > 1) {
                if (Version.TryParse(parts[0], out _)) {
                    _versionInfo!.Patchers.Add(new PatcherInfo {
                        Id = "optifine",
                        Version = parts[1]
                    });
                }
            }
        }

        // LabyMod
        try {
            // 使用 FirstOrDefault() 查找符合条件的节点
            var labyModNode = _versionJson!["arguments"]!["game"]!.AsArray()
                .FirstOrDefault(node =>
                    node!.GetValueKind() == JsonValueKind.String &&
                    node.ToString().Contains("labymod", StringComparison.OrdinalIgnoreCase));
            if (labyModNode != null) {
                _versionInfo!.Patchers.Add(new PatcherInfo {
                    Id = "labymod"
                });
            }
        } catch {
            LogWrapper.Info("未识别到 LabyMod");
        }
    }

    private void FindArgumentData(string argument, string id) {
        var args = _versionJson!["arguments"]!["game"]!.AsArray();
        var index = args.IndexOf(argument);
        var version = args[index + 1];
        _versionInfo!.Patchers.Add(new PatcherInfo {
            Id = id,
            Version = version!.ToString()
        });
    }

    /// <summary>
    /// 从 HashSet 中查找以指定前缀开头的 name 并提取版本号
    /// </summary>
    /// <param name="prefix">目标前缀</param>
    /// <returns>匹配的版本号或如果未找到</returns>
    private string? FindPatcherVersionsInHashSet(string prefix) {
        return _libraryNameHashCache!.Where(name => name.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
            .Select(name => name[(prefix.Length + 1)..])
            .FirstOrDefault();
    }

    /// <summary>
    /// 从 JSON 提取 libraries 的 name 属性为 HashSet
    /// </summary>
    private void ParseLibraryNamesAsHashSet() {
        _libraryNameHashCache = _libraries!.Where(lib => lib.Name != null)
            .Select(lib => lib.Name!)
            .ToHashSet();
    }

    #endregion

    #region Display Type

    /// <summary>
    /// 强制实例分类
    /// </summary>
    public McInstanceCardType DisplayType {
        set => _cachedDisplayType = value;
    }

    public McInstanceCardType GetInstanceDisplayType() {
        if (_cachedDisplayType == null) {
            RefreshInstanceDisplayType();
        }
        return _cachedDisplayType!.Value;
    }

    /// <summary>
    /// 实例分类
    /// </summary>
    private void RefreshInstanceDisplayType() {
        var savedDisplayType = (McInstanceCardType)SetupService.GetInt32(SetupEntries.Instance.DisplayType, Path);

        // 如果不是自动分类，跳过以下分类流程
        if (savedDisplayType != McInstanceCardType.Auto) {
            _cachedDisplayType = savedDisplayType;
            return;
        }

        var versionInfo = GetVersionInfo();

        // 判断各个可安装模组的实例
        _cachedDisplayType = McInstanceUtils.RecognizeInstanceCardType(versionInfo!);

        if (_cachedDisplayType != null) {
            return;
        }

        if (versionInfo!.Patchers.Count > 0) {
            _cachedDisplayType = McInstanceCardType.UnknownPatchers;
        } else {
            // 没有任何附加组件，按原版分类
            _cachedDisplayType = versionInfo.VersionType switch {
                McVersionType.Release => McInstanceCardType.Release,
                McVersionType.Snapshot => McInstanceCardType.Snapshot,
                McVersionType.Fool => McInstanceCardType.Fool,
                McVersionType.Old => McInstanceCardType.Old,
                _ => McInstanceCardType.UnknownPatchers
            };
        }
    }

    #endregion

    #region Java Version

    public async Task<(Version MinVer, Version MaxVer)> GetCompatibleJavaVersionRange() {
        var minVer = new Version(0, 0, 0, 0);
        var maxVer = new Version(999, 999, 999, 999);

        CheckJavaVersion(out var minVerVanilla, out var maxVerVanilla);
        minVer = minVerVanilla ?? minVer;
        maxVer = maxVerVanilla ?? maxVer;

        if (_versionInfo is null || !_versionInfo.IsNormalVersion) {
            return (minVer, maxVer);
        }

        // Minecraft jar recommendations
        var versionJsonInJar = await GetVersionJsonInJar();
        if (versionJsonInJar != null) {
            if (versionJsonInJar.TryGetPropertyValue("java_version", out var javaVersionNodeInJar) &&
                javaVersionNodeInJar?.GetValueKind() == JsonValueKind.Number) {
                var recommendedJava = javaVersionNodeInJar.GetValue<int>();
                McLaunchUtils.Log($"Mojang (in JAR) recommends Java {recommendedJava}");
                if (recommendedJava >= 22) {
                    minVer = UpdateMin(minVer, new Version(recommendedJava, 0, 0, 0));
                }
            }
        }

        // OptiFine adjustments
        if (_versionInfo.HasPatcher("optifine")) {
            if (_versionInfo.McVersion < new Version(1, 7) || _versionInfo.McVersionMinor == 12) {
                maxVer = UpdateMaxAndLog(maxVer, new Version(8, 999, 999, 999),
                    "OptiFine <1.7 / 1.12 requires max Java 8");
            } else if (_versionInfo.McVersion >= new Version(1, 8) && _versionInfo.McVersion < new Version(1, 12)) {
                LogWrapper.Debug("Launch", "OptiFine 1.8 - 1.11 requires exactly Java 8");
                minVer = UpdateMin(minVer, new Version(1, 8, 0, 0));
                maxVer = UpdateMax(maxVer, new Version(8, 999, 999, 999));
            }
        }

        // LiteLoader adjustments
        if (_versionInfo.HasPatcher("liteloader")) {
            maxVer = UpdateMaxAndLog(maxVer, new Version(8, 999, 999, 999),
                "LiteLoader requires max Java 8");
        }

        // Forge adjustments
        if (_versionInfo.HasPatcher("forge")) {
            var mcMinor = _versionInfo.McVersionMinor;
            var mcVersion = _versionInfo.McVersion;

            if (mcVersion >= new Version(1, 6, 1) && mcVersion <= new Version(1, 7, 2)) {
                LogWrapper.Debug("Launch", "1.6.1 - 1.7.2 Forge requires exactly Java 7");
                minVer = UpdateMin(minVer, new Version(1, 7, 0, 0));
                maxVer = UpdateMax(maxVer, new Version(1, 7, 999, 999));
            } else {
                var (logMessage, newMin, newMax) = mcMinor switch {
                    <= 12 => ("<=1.12 Forge requires Java 8", null, new Version(8, 999, 999, 999)),
                    <= 14 => ("1.13 - 1.14 Forge requires Java 8 - 10", new Version(1, 8, 0, 0), new Version(10, 999, 999, 999)),
                    15 => ("1.15 Forge requires Java 8 - 15", new Version(1, 8, 0, 0), new Version(15, 999, 999, 999)),
                    16 when Version.TryParse(_versionInfo.GetPatcher("forge")?.Version, out var forgeVersion)
                            && forgeVersion > new Version(34, 0, 0)
                            && forgeVersion < new Version(36, 2, 25) =>
                        ("1.16 Forge 34.X - 36.2.25 requires max Java 8u321", null, new Version(1, 8, 0, 321)),
                    18 when _versionInfo.HasPatcher("optifine") =>
                        ("1.18 Forge + OptiFine requires max Java 18", null, new Version(18, 999, 999, 999)),
                    _ => (null, null, null) // 默认情况，不匹配任何规则
                };

                if (logMessage != null) {
                    LogWrapper.Debug("Launch", logMessage);
                    if (newMin != null) {
                        minVer = UpdateMin(minVer, newMin);
                    }
                    if (newMax != null) {
                        maxVer = UpdateMax(maxVer, newMax);
                    }
                }
            }
        }

        // Cleanroom adjustments
        if (_versionInfo.HasPatcher("cleanroom")) {
            minVer = UpdateMinAndLog(minVer, new Version(21, 0, 0, 0),
                "Cleanroom requires min Java 21");
        }

        // Fabric adjustments
        if (_versionInfo.HasPatcher("fabric")) {
            var mcMinor = _versionInfo.McVersionMinor;
            // 根据 mcMinor 版本号，使用 switch 表达式确定最低 Java 版本
            minVer = mcMinor switch {
                >= 15 and <= 16 => UpdateMinAndLog(minVer, new Version(1, 8, 0, 0), 
                    "1.15 - 1.16 Fabric requires min Java 8"),
                >= 18 => UpdateMinAndLog(minVer, new Version(17, 0, 0, 0), 
                    "1.18+ Fabric requires min Java 17"),
                _ => minVer // 默认情况，不更新
            };
        }

        // LabyMod adjustments
        if (_versionInfo.HasPatcher("labymod")) {
            minVer = UpdateMinAndLog(minVer, new Version(21, 0, 0, 0), 
                "LabyMod requires min Java 21");
            maxVer = new Version(999, 999, 999, 999); // Reset max if needed, but already high
        }

        // JSON recommended version
        if (_versionJson is null || 
            !_versionJson.TryGetPropertyValue("javaVersion", out var javaVersionNode) ||
            javaVersionNode?.GetValueKind() != JsonValueKind.Object ||
            !javaVersionNode.AsObject().TryGetPropertyValue("majorVersion", out var majorVersionElement) ||
            majorVersionElement?.GetValueKind() != JsonValueKind.Number) {
    
            return (minVer, maxVer);
        }

        // All checks passed, proceed with the main logic
        var jsonRecommendedJava = majorVersionElement.GetValue<int>();
        McLaunchUtils.Log($"Mojang recommends Java {jsonRecommendedJava}");
        if (jsonRecommendedJava >= 22) {
            minVer = UpdateMin(minVer, new Version(jsonRecommendedJava, 0, 0, 0));
        }

        return (minVer, maxVer);
    }

    // Helper to update minVer to the higher value
    private static Version UpdateMin(Version current, Version candidate) => candidate > current ? candidate : current;

    // Helper to update maxVer to the lower value
    private static Version UpdateMax(Version current, Version candidate) => candidate < current ? candidate : current;

    // 辅助方法：负责更新版本并打印日志
    private static Version UpdateMinAndLog(Version currentMin, Version newMin, string logMessage) {
        LogWrapper.Debug("Launch", logMessage);
        return UpdateMin(currentMin, newMin);
    }
    
    private static Version UpdateMaxAndLog(Version currentMax, Version newMax, string logMessage) {
        LogWrapper.Debug("Launch", logMessage);
        return UpdateMax(currentMax, newMax);
    }

    // 定义 Java 版本要求规则
    private static readonly List<(Func<McInstanceInfo, bool> Condition, Version MinVer, Version? MaxVer, string LogMessage)> VanillaJavaVersionRules = [
        // 1.20.5+ (24w14a+)：至少 Java 21
        (
            info => !info.IsNormalVersion && info.ReleaseTime >= new DateTime(2024, 4, 2) ||
                    info.IsNormalVersion && info.McVersion >= new Version(1, 20, 5),
            new Version(21, 0, 0, 0),
            null,
            "MC 1.20.5+ (24w14a+) 要求至少 Java 21"
        ),
        // 1.18 pre2+：至少 Java 17
        (
            info => !info.IsNormalVersion && info.ReleaseTime >= new DateTime(2021, 11, 16) ||
                    info.IsNormalVersion && info.McVersion >= new Version(1, 18),
            new Version(17, 0, 0, 0),
            null,
            "MC 1.18 pre2+ 要求至少 Java 17"
        ),
        // 1.17+ (21w19a+)：至少 Java 16
        (
            info => !info.IsNormalVersion && info.ReleaseTime >= new DateTime(2021, 5, 11) ||
                    info.IsNormalVersion && info.McVersion >= new Version(1, 17),
            new Version(16, 0, 0, 0),
            null,
            "MC 1.17+ (21w19a+) 要求至少 Java 16"
        ),
        // 1.12+：至少 Java 8
        (
            info => info.ReleaseTime.Year >= 2017,
            new Version(1, 8, 0, 0),
            null,
            "MC 1.12+ 要求至少 Java 8"
        ),
        // 1.5.2-：最高 Java 12
        (
            info => info.ReleaseTime <= new DateTime(2013, 5, 1) && info.ReleaseTime.Year >= 2001,
            new Version(1, 8, 0, 0), // 假设最低 Java 8（可调整）
            new Version(12, 999, 999, 999),
            "MC 1.5.2- 要求最高 Java 12"
        )
    ];

    /// <summary>
    /// 检查 Minecraft 版本所需的 Java 版本
    /// </summary>
    /// <param name="minVer">输出：所需最低 Java 版本</param>
    /// <param name="maxVer">输出：所需最高 Java 版本（可能为 null）</param>
    /// <returns>返回 true 表示找到匹配规则，false 表示未匹配</returns>
    private void CheckJavaVersion(out Version? minVer, out Version? maxVer) {
        // 使用 FirstOrDefault 查找第一个匹配的规则
        var matchedRule = VanillaJavaVersionRules.FirstOrDefault(rule => rule.Condition(_versionInfo!));

        // 检查元组中的 Condition 委托是否为 null，来判断是否找到了匹配项
        if (matchedRule.Condition != null) {
            LogWrapper.Debug("Launch", matchedRule.LogMessage);

            minVer = matchedRule.MinVer;
            maxVer = matchedRule.MaxVer;
        }

        // 默认值：未匹配任何规则
        LogWrapper.Debug("Launch", "未匹配任何 Java 版本规则，使用默认值");
        minVer = new Version(1, 8, 0, 0); // 默认最低 Java 8
        maxVer = null;
    }

    #endregion

    #region Version Json Info

    /// <summary>
    /// 实例信息
    /// </summary>
    public McInstanceInfo? GetVersionInfo() {
        return _versionInfo ??= RefreshVersionInfo();
    }

    private McInstanceInfo? RefreshVersionInfo() {
        var versionInfo = new McInstanceInfo();
        if (_cachedDisplayType == McInstanceCardType.Error) {
            return null;
        }

        // 获取 MC 版本
        var version = McInstanceUtils.RecognizeMcVersion(_versionJson!);

        if (version != null) {
            versionInfo.McVersionStr = version;
        } else {
            LogWrapper.Warn("识别 Minecraft 版本时出错");
            versionInfo.McVersionStr = "Unknown";
            Desc = "无法识别 Minecraft 版本";
        }

        // 获取发布时间
        var releaseTime = McInstanceUtils.RecognizeReleaseTime(_versionJson!);
        versionInfo.ReleaseTime = releaseTime;

        // 获取版本类型
        versionInfo.VersionType = McInstanceUtils.RecognizeVersionType(_versionJson!, releaseTime);

        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
        try {
            if (IsPatchesFormatJson) {
                foreach (var patch in _versionJson!["patches"]!.AsArray()) {
                    var patcherInfo = patch.Deserialize<PatcherInfo>(options);
                    if (patcherInfo != null) {
                        versionInfo.Patchers.Add(patcherInfo);
                    }
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            versionInfo.McVersionStr = "Unknown";
            Desc = $"无法识别：{ex.Message}";
        }
        _versionInfo = versionInfo;

        return _versionInfo;
    }

    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    public async Task<JsonObject?> GetVersionJsonAsync() {
        return _versionJson ?? await RefreshVersionJsonAsync();
    }

    public async Task<JsonObject?> RefreshVersionJsonAsync() {
        var jsonPath = System.IO.Path.Combine(Path, $"{Name}.json");
        if (!File.Exists(jsonPath)) {
            var jsonFiles = Directory.GetFiles(Path, "*.json");
            if (jsonFiles.Length == 1) {
                jsonPath = jsonFiles[0];
            } else {
                return null;
            }
        }

        try {
            // 异步读取文件内容
            await using var fileStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var jsonNode = await JsonNode.ParseAsync(fileStream);
            var jsonObject = jsonNode!.AsObject();

            // 处理 Patches 格式 JSON
            if (jsonObject["patches"] != null) {
                IsPatchesFormatJson = true;
            }

            _versionJson = jsonObject; // 保存到字段
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"初始化实例 JSON 失败（{Name}）");
        }

        return _versionJson;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public bool IsOldJson => _versionJson?["minecraftArguments"]?.ToString() is not null;

    /// <summary>
    /// 是否为 Patches 格式 JSON
    /// </summary>
    public bool IsPatchesFormatJson { get; set; }

    #endregion

    #region Check and Load

    public async Task Check() {
        if (!await CheckPermission() || !await CheckJson()) {
            DisplayType = McInstanceCardType.Error;
            Logo = System.IO.Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
        } else {
            // 确定实例图标
            Logo = McInstanceLogic.DetermineLogo(this);
        }
    }

    public async Task<bool> CheckPermission() {
        if (!Directory.Exists(Path)) {
            Desc = $"未找到实例 {Name}";
            return false;
        }

        try {
            Directory.CreateDirectory(Path + "PCL\\");
            await Directories.CheckPermissionWithExceptionAsync(Path + "PCL\\");
        } catch (Exception ex) {
            Desc = "PCL 没有对该文件夹的访问权限，请以管理员身份运行";
            LogWrapper.Warn(ex, "没有访问实例文件夹的权限");
            return false;
        }
        return true;
    }

    public async Task<bool> CheckJson() {
        var versionJson = await GetVersionJsonAsync();
        if (versionJson == null) {
            LogWrapper.Warn($"实例 JSON 可用性检查失败（{Path}）");
            Desc = "实例 JSON 不存在或无法解析";
            return false;
        }

        return true;
    }

    public void Load() {
        GetVersionInfo();
        GetInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    public async Task Refresh() {
        await RefreshVersionJsonAsync();

        RefreshVersionInfo();
        RefreshInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    private void SetDescriptiveInfo() {
        // 确定实例描述和状态
        Desc = string.IsNullOrEmpty(SetupService.GetString(SetupEntries.Instance.CustomInfo, Path))
            ? McInstanceLogic.GetDefaultDescription(this)
            : SetupService.GetString(SetupEntries.Instance.CustomInfo, Path);
        IsFavorited = SetupService.GetBool(SetupEntries.Instance.Starred, Path);

        // 写入缓存
        SetupService.SetString(SetupEntries.Instance.Info, Desc, Path);
        SetupService.SetString(SetupEntries.Instance.LogoPath, Logo!, Path);
    }

    #endregion

    #region Libraries

    // 从完整JSON中提取并反序列化libraries字段
    public List<Library>? ParseLibrariesFromJson() {
        try {
            // 获取libraries字段
            var librariesNode = _versionJson!["libraries"];
            if (librariesNode == null) {
                Console.WriteLine("JSON中未找到libraries字段");
                _libraries = null;
                return null;
            }

            // 反序列化libraries字段
            var librariesJson = librariesNode.ToJsonString();
            _libraries = LibraryDeserializer.DeserializeLibraries(librariesJson);
            return _libraries;
        } catch (JsonException ex) {
            Console.WriteLine($"JSON解析或反序列化错误: {ex.Message}");
            _libraries = null;
            return null;
        }
    }

    #endregion

    #region Asset Index

    /// <summary>
    /// 从完整JSON中提取并反序列化assetIndex字段
    /// </summary>
    /// <returns>反序列化后的AssetIndex对象，如果不存在或失败则返回null</returns>
    public AssetIndex? ParseAssetIndexFromJson() {
        try {
            var assetIndexNode = _versionJson!["assetIndex"];
            if (assetIndexNode == null) {
                LogWrapper.Warn("JSON中未找到assetIndex字段");
                _assetIndex = null;
                return null;
            }

            var assetIndexJson = assetIndexNode.ToJsonString();
            _assetIndex = AssetIndexDeserializer.DeserializeAssetIndex(assetIndexJson);
            return _assetIndex;
        } catch (JsonException ex) {
            LogWrapper.Warn(ex, "JSON解析或反序列化错误");
            _assetIndex = null;
            return null;
        }
    }

    #endregion

    /// <summary>
    /// 实例 JAR 中的 version.json 文件对象
    /// </summary>
    public async Task<JsonObject?> GetVersionJsonInJar() {
        if (_versionJsonInJar != null) {
            return _versionJsonInJar;
        }

        var jarPath = $"{Path}{Name}.jar";
        if (!File.Exists(jarPath)) {
            return null;
        }

        try {
            await using var fileStream = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var zipFile = new ZipFile(fileStream); // SharpZipLib 的 ZipFile

            var versionJsonEntry = zipFile.GetEntry("version.json");
            if (versionJsonEntry != null) {
                await using var entryStream = zipFile.GetInputStream(versionJsonEntry);
                var jsonNode = await JsonNode.ParseAsync(entryStream);
                if (jsonNode is JsonObject jsonObj) {
                    _versionJsonInJar = jsonObj; // 保存到字段
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "从实例 JAR 中读取 version.json 失败");
        }
        return _versionJsonInJar;
    }
}

public enum McInstanceCardType {
    Auto, // Used only for forcing automatic instance classification

    // PCL 逻辑版本类型
    Star,
    Custom,
    Hidden,

    // Patchers 类型版本
    Modded,
    NeoForge,
    Fabric,
    Forge,
    Quilt,
    LegacyFabric,
    Cleanroom,
    LiteLoader,

    Client,
    OptiFine,
    LabyMod,

    // 正常 MC 版本类型
    Release,
    Snapshot,
    Fool,
    Old,

    UnknownPatchers,

    Error
}
