using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.Minecraft.McInstance.Resources;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public class McInstance {
    private JsonObject? _versionJson;
    private McInstanceInfo? _versionInfo;

    private List<Library>? _libraries; // 依赖库列表
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
            versionInfo.McVersion = version;
        } else {
            LogWrapper.Warn("识别 Minecraft 版本时出错");
            versionInfo.McVersion = "Unknown";
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
            versionInfo.McVersion = "Unknown";
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
        if (_versionJson != null) {
            return _versionJson;
        }

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
    public bool GetIsOldJsonAsync() => _versionJson?["minecraftArguments"]?.ToString() is not null;

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

    public McInstance Load() {
        GetVersionInfo();
        GetInstanceDisplayType();

        SetDescriptiveInfo();

        return this;
    }

    public McInstance Refresh() {
        RefreshVersionInfo();
        RefreshInstanceDisplayType();

        SetDescriptiveInfo();

        return this;
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
