using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Folder;
using PCL.Core.Minecraft.Instance.Handler;
using PCL.Core.Minecraft.Instance.Resources;
using PCL.Core.Minecraft.Launch;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.Instance;

/// <summary>
/// 管理实例基础信息
/// </summary>
public class McInstance {
    private JsonObject? _versionJson;
    private McInstanceInfo? _instanceInfo;

    private List<Library>? _libraries; // 依赖库列表
    private HashSet<string>? _libraryNameHashCache; // 依赖库哈希缓存
    private AssetIndex? _assetIndex;

    private JsonObject? _versionJsonInJar;

    private McInstanceCardType? _cachedDisplayType;

    private readonly InstanceIsolationHandler _instanceIsolationHandler;
    private readonly InstanceJsonHandler _instanceJsonHandler;
    private readonly InstanceUiHandler _instanceUiHandler;
    private readonly InstanceJavaHandler _instanceJavaHandler;

    /// <summary>
    /// 初始化 Minecraft 实例
    /// 初始化后请一定要先运行 Check() 方法
    /// 在你调用其他方法时，我们默认你已经调用了 Check() 并且通过了检查
    /// </summary>
    /// <param name="path"></param>
    public McInstance(string path) {
        Path = (path.Contains(':') ? "" : McFolderManager.PathMcFolder + "versions\\") + path + (path.EndsWith('\\') ? "" : "\\");
        _instanceIsolationHandler = new InstanceIsolationHandler(Path, Name, _cachedDisplayType, _instanceInfo);
        _instanceJsonHandler = new InstanceJsonHandler(Path, Name);
        _instanceUiHandler = new InstanceUiHandler(Path, _instanceInfo, _cachedDisplayType);
        _instanceJavaHandler = new InstanceJavaHandler(_instanceInfo, _versionJson, _versionJsonInJar);
    }

    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 应用版本隔离后的 Minecraft 根文件夹路径，以“\”结尾
    /// </summary>
    public string? IsolatedPath {
        get {
            if (_instanceInfo == null) {
                GetInstanceInfo();
            }
            return _instanceIsolationHandler.GetIsolatedPath();
        }
    }

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
                _instanceInfo!.Patchers.Add(new PatcherInfo {
                    Id = pair.Value,
                    Version = version
                });
            }
        }

        var hasNeoForge = true;

        // NeoForge
        if (FindPatcherVersionsInHashSet("net.neoforged.fancymodloader") != null) {
            try {
                if (_instanceInfo!.McVersionStr.IsNullOrEmpty()) {
                    try {
                        FindArgumentData("--fml.neoForgeVersion", "neoforge");
                    } catch {
                        FindArgumentData("--fml.forgeVersion", "neoforge");
                    }
                }
                FindArgumentData(_instanceInfo!.McVersionStr == "1.20.1" ? "--fml.forgeVersion" : "--fml.neoForgeVersion", "neoforge");

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
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
                        Id = "legacyfabric",
                        Version = fabricVersion
                    });
                } else {
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
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
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
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
                _instanceInfo!.Patchers.Add(new PatcherInfo {
                    Id = "labymod"
                });
            }
        } catch {
            LogWrapper.Info("未识别到 LabyMod");
        }
        
        // Game
        _instanceInfo!.Patchers.Add(new PatcherInfo {
            Id = "game",
            Version = _instanceInfo.McVersionStr
        });
    }

    private void FindArgumentData(string argument, string id) {
        var args = _versionJson!["arguments"]!["game"]!.AsArray();
        var index = args.IndexOf(argument);
        var version = args[index + 1];
        _instanceInfo!.Patchers.Add(new PatcherInfo {
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
        var savedDisplayType = (McInstanceCardType)Config.Instance.DisplayType[Path];

        // 如果不是自动分类，跳过以下分类流程
        if (savedDisplayType != McInstanceCardType.Auto) {
            _cachedDisplayType = savedDisplayType;
            return;
        }

        var versionInfo = GetInstanceInfo();

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

    public async Task<(Version MinVer, Version MaxVer)> GetCompatibleJavaVersionRangeAsync() {
        await GetVersionJsonInJarAsync();
        return _instanceJavaHandler.GetCompatibleJavaVersionRange();
    }

    #region Version Json Info

    /// <summary>
    /// 实例信息
    /// </summary>
    public McInstanceInfo? GetInstanceInfo() {
        return _instanceInfo ??= RefreshInstanceInfo();
    }

    private McInstanceInfo? RefreshInstanceInfo() {
        var instanceInfo = new McInstanceInfo();
        if (_cachedDisplayType == McInstanceCardType.Error) {
            return null;
        }

        // 获取 MC 版本
        var version = McInstanceUtils.RecognizeMcVersion(_versionJson!);

        if (version != null) {
            instanceInfo.McVersionStr = version;
        } else {
            LogWrapper.Warn("识别 Minecraft 版本时出错");
            instanceInfo.McVersionStr = "Unknown";
            Desc = "无法识别 Minecraft 版本";
        }

        // 获取发布时间
        var releaseTime = McInstanceUtils.RecognizeReleaseTime(_versionJson!);
        instanceInfo.ReleaseTime = releaseTime;

        // 获取版本类型
        instanceInfo.VersionType = McInstanceUtils.RecognizeVersionType(_versionJson!, releaseTime);
        
        try {
            if (IsPatchesFormatJson) {
                foreach (var patch in _versionJson!["patches"]!.AsArray()) {
                    var patcherInfo = patch.Deserialize<PatcherInfo>(Files.PrettierJsonOptions);
                    if (patcherInfo != null) {
                        instanceInfo.Patchers.Add(patcherInfo);
                    }
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            instanceInfo.McVersionStr = "Unknown";
            Desc = $"无法识别：{ex.Message}";
        }
        _instanceInfo = instanceInfo;

        return _instanceInfo;
    }

    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    public async Task<JsonObject?> GetVersionJsonAsync() {
        return _versionJson ?? await RefreshVersionJsonAsync();
    }

    public async Task<JsonObject?> RefreshVersionJsonAsync() {
        _versionJson = await _instanceJsonHandler.RefreshVersionJsonAsync();
        return _versionJson;
    }
    
    /// <summary>
    /// 异步获取 Jar 中的 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 Jar 中的 JSON 对象。</returns>
    public async Task<JsonObject?> GetVersionJsonInJarAsync() {
        return _versionJsonInJar ?? await RefreshVersionJsonAsync();
    }

    public async Task<JsonObject?> RefreshVersionJsonInJarAsync() {
        _versionJsonInJar = await _instanceJsonHandler.RefreshVersionJsonInJarAsync();
        return _versionJsonInJar;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public bool IsOldJson => _versionJson!.ContainsKey("minecraftArguments");

    /// <summary>
    /// 是否为 Patches 格式 JSON
    /// </summary>
    public bool IsPatchesFormatJson => _versionJson!.ContainsKey("patches");

    #endregion

    #region Check and Load

    public async Task CheckAsync() {
        if (!await CheckPermissionAsync() || !await CheckJsonAsync()) {
            DisplayType = McInstanceCardType.Error;
            Logo = System.IO.Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
        } else {
            // 确定实例图标
            Logo = _instanceUiHandler.GetLogo();
        }
    }

    private async Task<bool> CheckPermissionAsync() {
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

    private async Task<bool> CheckJsonAsync() {
        await GetVersionJsonAsync();
        if (_versionJson == null) {
            LogWrapper.Warn($"实例 JSON 可用性检查失败（{Path}）");
            Desc = "实例 JSON 不存在或无法解析";
            return false;
        }
        
        GetInstanceInfo();
        if (_instanceInfo == null) {
            LogWrapper.Warn($"实例信息检查失败（{Path}）");
            Desc = "无法识别实例信息";
            return false;
        }

        return true;
    }

    public void Load() {
        GetInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    public async Task RefreshAsync() {
        await RefreshVersionJsonAsync();

        RefreshInstanceInfo();
        RefreshInstanceDisplayType();

        SetDescriptiveInfo();

        ParseLibrariesFromJson();
        ParseAssetIndexFromJson();

        ConvertToPatches();
    }

    private void SetDescriptiveInfo() {
        // 确定实例描述和状态
        Desc = _instanceUiHandler.GetDescription();
        IsFavorited = Config.Instance.Starred[Path];

        // 写入缓存
        Config.Instance.Info[Path] = Desc;
        Config.Instance.LogoPath[Path] = Logo!;
    }

    #endregion

    #region Libraries

    public List<Library>? Libraries => _libraries ?? ParseLibrariesFromJson();

    // 从完整JSON中提取并反序列化libraries字段
    private List<Library>? ParseLibrariesFromJson() {
        try {
            // 获取libraries字段
            var librariesNode = _versionJson!["libraries"];
            if (librariesNode == null) {
                Console.WriteLine("JSON中未找到libraries字段");
                _libraries = null;
                return null;
            }

            // 反序列化libraries字段
            _libraries = LibraryDeserializer.DeserializeLibraries(librariesNode);
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
            
            _assetIndex = AssetIndexDeserializer.DeserializeAssetIndex(assetIndexNode);
            return _assetIndex;
        } catch (JsonException ex) {
            LogWrapper.Warn(ex, "JSON解析或反序列化错误");
            _assetIndex = null;
            return null;
        }
    }

    #endregion
}
