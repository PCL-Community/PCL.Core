using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public class McInstance {
    private string? _name;
    private JsonObject? _versionJson;
    private McInstanceInfo? _version;
    private JsonObject? _versionJsonInJar;
    
    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 应用版本隔离后的 Minecraft 根文件夹路径，以“\”结尾
    /// </summary>
    public async Task<string> GetPathIndie() => await McInstanceUtils.GetIsolatedPathAsync(this);
    
    /// <summary>
    /// 实例文件夹名称
    /// </summary>
    public string Name => _name ??= string.IsNullOrEmpty(Path) ? "" : new DirectoryInfo(Path).Name;
    
    /// <summary>
    /// 实例发布时间
    /// </summary>
    public DateTime ReleaseTime { get; set; } = new DateTime(1970, 1, 1, 15, 0, 0);

    /// <summary>
    /// 显示的描述文本
    /// </summary>
    public string Info { get; set; } = "该实例未被加载，请向作者反馈此问题";

    /// <summary>
    /// 实例状态
    /// </summary>
    public McInstanceState State { get; set; } = McInstanceState.Error;

    /// <summary>
    /// 显示的实例图标
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 是否为收藏的实例
    /// </summary>
    public bool IsStar { get; set; }

    /// <summary>
    /// 强制实例分类
    /// </summary>
    public McInstanceCardType DisplayType { get; set; } = McInstanceCardType.Auto;

    /// <summary>
    /// 是否可安装 Mod
    /// </summary>
    public async Task<bool> GetIsModable() => await McInstanceUtils.GetIsModableAsync(this);

    /// <summary>
    /// 实例信息
    /// </summary>
    public async Task<McInstanceInfo> GetVersionAsync() {
        if (_version == null) {
            _version = new McInstanceInfo();
            try {
                // 获取发布时间并判断是否为老版本
                var releaseTime = await McInstanceUtils.TryGetReleaseTimeAsync(this);
                _version.McName = releaseTime <= new DateTime(2011, 11, 16) ? "Old" : McInstanceUtils.GetVersionFromJson(this);
                ReleaseTime = releaseTime;
            } catch (Exception ex) {
                LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
                _version.McName = "Unknown";
                Info = $"无法识别：{ex.Message}";
            }
        }
        return _version;
    }
    
    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    /// <exception cref="Exception">如果初始化 JSON 失败或版本依赖项出现嵌套，则抛出异常。</exception>
    public async Task<JsonObject> GetVersionJsonAsync() {
        if (_versionJson == null) {
            string jsonPath = System.IO.Path.Combine(Path, $"{Name}.json");
            if (!File.Exists(jsonPath))
            {
                string[] jsonFiles = Directory.GetFiles(Path, "*.json");
                if (jsonFiles.Length == 1)
                {
                    jsonPath = jsonFiles[0];
                }
            }

            try {
                // 异步读取文件内容
                await using var fileStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var jsonNode = await JsonNode.ParseAsync(fileStream);
                var jsonObject = jsonNode.AsObject();

                // 处理 HMCL 格式 JSON
                if (jsonObject["patches"] != null && jsonObject["time"] == null) {
                    IsPatchesFormatJson = true;
                }
                
                _versionJson = jsonObject; // 保存到字段
            } catch (Exception ex) {
                throw new Exception($"初始化实例 JSON 失败（{Name ?? "null"}）", ex);
            }
        }

        return _versionJson;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public async Task<bool> GetIsOldJsonAsync() => (await GetVersionJsonAsync())["minecraftArguments"]?.ToString() != null;

    /// <summary>
    /// 是否为 Patches 格式 JSON
    /// </summary>
    public bool IsPatchesFormatJson { get; set; }

    /// <summary>
    /// 实例 JAR 中的 version.json 文件对象
    /// </summary>
    public async Task<JsonObject?> GetVersionJsonInJar() {
        if (_versionJsonInJar == null) {
            string jarPath = $"{Path}{Name}.jar";
            if (File.Exists(jarPath)) {
                try {
                    await using var fileStream = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var zipFile = new ZipFile(fileStream); // SharpZipLib 的 ZipFile

                    var versionJsonEntry = zipFile.GetEntry("version.json");
                    if (versionJsonEntry != null) {
                        await using var entryStream = zipFile.GetInputStream(versionJsonEntry);
                        JsonNode? jsonNode = await JsonNode.ParseAsync(entryStream);
                        if (jsonNode is JsonObject jsonObj) {
                            _versionJsonInJar = jsonObj; // 保存到字段
                        }
                    }
                } catch (Exception ex) {
                    LogWrapper.Warn(ex, "从实例 JAR 中读取 version.json 失败");
                }
            }
        }
        return _versionJsonInJar;
    }

    public bool IsLoaded { get; private set; }

    public McInstance(string path) {
        Path = (path.Contains(":") ? "" : McFolderManager.PathMcFolder + "versions\\") + path + (path.EndsWith("\\") ? "" : "\\");
    }

    public async Task<bool> Check() {
        if (!Directory.Exists(Path)) {
            State = McInstanceState.Error;
            Info = $"未找到实例 {Name}";
            return false;
        }

        try {
            Directory.CreateDirectory(Path + "PCL\\");
            await Directories.CheckPermissionWithExceptionAsync(Path + "PCL\\");
        } catch (Exception ex) {
            State = McInstanceState.Error;
            Info = "PCL 没有对该文件夹的访问权限，请以管理员身份运行";
            LogWrapper.Warn(ex, "没有访问实例文件夹的权限");
            return false;
        }

        try {
            _ = await GetVersionJsonAsync();
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"实例 JSON 可用性检查失败（{Path}）");
            _versionJson = null;
            Info = ex.Message;
            State = McInstanceState.Error;
            return false;
        }

        State = McInstanceState.Original;
        return true;
    }

    public async Task<McInstance> Load() {
        try {
            var version = await GetVersionAsync();
            if (await Check()) {
                // 确定实例分类
                switch (version.McName) {
                    case "Unknown":
                        State = McInstanceState.Error;
                        break;
                    case "Old":
                        State = McInstanceState.Old;
                        break;
                    default:
                        await McInstanceUtils.DetermineInstanceTypeAsync(this);
                        break;
                }
            }

            
            // 确定实例图标
            Logo = DetermineLogo();

            // 确定实例描述和状态
            Info = string.IsNullOrEmpty(SetupService.GetString(SetupEntries.Instance.CustomInfo, Path))
                ? await McInstanceUtils.GetDefaultDescription(this)
                : SetupService.GetString(SetupEntries.Instance.CustomInfo, Path);
            SetupService.SetString(SetupEntries.Instance.Starred, Path);
            IsStar = SetupService.GetBool(SetupEntries.Instance.Starred, Path);
            DisplayType = (McInstanceCardType) SetupService.GetInt32(SetupEntries.Instance.DisplayType, Path);

            // 写入缓存
            if (Directory.Exists(Path)) {
                SetupService.SetInt32(SetupEntries.Instance.State, (int) State, Path);
                SetupService.SetString(SetupEntries.Instance.Info, Info, Path);
                SetupService.SetString(SetupEntries.Instance.LogoPath, Logo, Path);
            }

            if (State != McInstanceState.Error) {
                SetupService.SetString(SetupEntries.Instance.ReleaseTime, ReleaseTime.ToString("yyyy-MM-dd HH:mm"), Path);
                SetupService.SetString(SetupEntries.Instance.FabricVersion, version.FabricVersion, Path);
                SetupService.SetString(SetupEntries.Instance.LegacyFabricVersion, version.LegacyFabricVersion, Path);
                SetupService.SetString(SetupEntries.Instance.QuiltVersion, version.QuiltVersion, Path);
                SetupService.SetString(SetupEntries.Instance.LabyModVersion, version.LabyModVersion, Path);
                SetupService.SetString(SetupEntries.Instance.OptiFineVersion, version.OptiFineVersion, Path);
                SetupService.SetBool(SetupEntries.Instance.HasLiteLoader, version.HasLiteLoader, Path);
                SetupService.SetString(SetupEntries.Instance.ForgeVersion, version.ForgeVersion, Path);
                SetupService.SetString(SetupEntries.Instance.NeoForgeVersion, version.NeoForgeVersion, Path);
                SetupService.SetString(SetupEntries.Instance.CleanroomVersion, version.CleanroomVersion, Path);
                SetupService.SetInt32(SetupEntries.Instance.SortCode, version.SortCode, Path);
                SetupService.SetString(SetupEntries.Instance.McVersion, version.McName, Path);
                SetupService.SetInt32(SetupEntries.Instance.VersionMajor, version.McCodeMain, Path);
                SetupService.SetInt32(SetupEntries.Instance.VersionMinor, version.McCodeSub, Path);
            }
        } catch (Exception ex) {
            Info = $"未知错误：{ex}";
            Logo = Basics.ImagePath + "Blocks/RedstoneBlock.png";
            State = McInstanceState.Error;
            LogWrapper.Warn(ex, $"加载实例失败（{Name}）");
        } finally {
            IsLoaded = true;
        }
        return this;
    }
    
    private string DetermineLogo() {
        var logo = SetupService.GetString(SetupEntries.Instance.LogoPath, Path);
        if (string.IsNullOrEmpty(logo) || !SetupService.GetBool(SetupEntries.Instance.IsLogoCustom, Path))
        {
            var field = typeof(McInstanceState).GetField(State.ToString());
            var iconPath = field?.GetCustomAttribute<IconPathAttribute>()?.Path ?? "Blocks/RedstoneBlock.png";
            logo = System.IO.Path.Combine(Basics.ImagePath, iconPath);
        }
        return logo;
    }
}

public enum McInstanceState {
    Error,
    [IconPath("Blocks/Grass.png")]
    Original,
    [IconPath("Blocks/CommandBlock.png")]
    Snapshot,
    [IconPath("Blocks/CobbleStone.png")]
    Old,
    [IconPath("Blocks/Anvil.png")]
    Forge,
    [IconPath("Blocks/NeoForge.png")]
    NeoForge,
    [IconPath("Blocks/Cleanroom.png")]
    Cleanroom,
    [IconPath("Blocks/Fabric.png")]
    Fabric,
    [IconPath("Blocks/Fabric.png")]
    LegacyFabric,
    [IconPath("Blocks/Quilt.png")]
    Quilt,
    [IconPath("Blocks/GrassPath.png")]
    OptiFine,
    [IconPath("Blocks/Egg.png")]
    LiteLoader,
    [IconPath("Blocks/GoldBlock.png")]
    Fool,
    [IconPath("Blocks/LabyMod.png")]
    LabyMod
}

public enum McInstanceCardType {
    Star = -1,
    Auto = 0, // Used only for forcing automatic instance classification
    Hidden = 1,
    API = 2,
    OriginalLike = 3,
    Rubbish = 4,
    Fool = 5,
    Error = 6
}

[AttributeUsage(AttributeTargets.Field)]
public class IconPathAttribute(string path) : Attribute {
    public string Path { get; } = path;
}