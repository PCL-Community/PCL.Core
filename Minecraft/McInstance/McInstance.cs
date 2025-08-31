using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public class McInstance {
    private readonly McInstanceIsolationHandler _isolationHandler;
    
    private string? _name;
    private string? _jsonText;
    private JsonObject? _versionJson;
    private McInstanceInfo? _version;
    private string? _inheritVersion;
    private JsonObject? _versionJsonInJar;

    public McInstance(string path, McInstanceIsolationHandler isolationHandler) {
        _isolationHandler = isolationHandler ?? throw new ArgumentNullException(nameof(isolationHandler));
    }
    
    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 应用版本隔离后的 Minecraft 根文件夹路径，以“\”结尾
    /// </summary>
    public string PathIndie => _isolationHandler.GetInstancePath(this);

    /// <summary>
    /// 实例文件夹名称
    /// </summary>
    public string Name => _name ??= string.IsNullOrEmpty(Path) ? "" : new DirectoryInfo(Path).Name;

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
    public bool Modable {
        get {
            if (!IsLoaded) Load();
            return Version.HasFabric || Version.HasLegacyFabric || Version.HasQuilt ||
                   Version.HasForge || Version.HasLiteLoader || Version.HasNeoForge ||
                   Version.HasCleanroom || DisplayType == McInstanceCardType.API;
        }
    }

    /// <summary>
    /// 实例信息
    /// </summary>
    public McInstanceInfo Version {
        get {
            if (_version == null) {
                _version = new McInstanceInfo();
                try {
                    // 获取发布时间并判断是否为老版本
                    _version.McName = TryGetReleaseTimeAndVersion(out DateTime releaseTime)
                        ? releaseTime <= new DateTime(2011, 11, 16) ? "Old" : GetVersionFromJson()
                        : "Unknown";
                    ReleaseTime = releaseTime;
                } catch (Exception ex) {
                    LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
                    _version.McName = "Unknown";
                    Info = $"无法识别：{ex.Message}";
                }
            }
            return _version;
        }
        set => _version = value;
    }

    /// <summary>
    /// 实例发布时间
    /// </summary>
    public DateTime ReleaseTime { get; set; } = new DateTime(1970, 1, 1, 15, 0, 0);
    
    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    /// <exception cref="Exception">如果初始化 JSON 失败或版本依赖项出现嵌套，则抛出异常。</exception>
    public async Task<JsonObject> GetJsonObjectAsync() {
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
                await using var fileStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read)
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
    /// 设置 JSON 对象。
    /// </summary>
    public JsonObject VersionJson {
        set => _versionJson = value;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public async Task<bool> GetIsOldJsonAsync() => (await GetJsonObjectAsync())["minecraftArguments"]?.ToString() != null;

    /// <summary>
    /// 是否为 Patches 格式 JSON
    /// </summary>
    public bool IsPatchesFormatJson { get; set; }

    /// <summary>
    /// 实例 JAR 中的 version.json 文件对象
    /// </summary>
    public async Task<JsonObject?> GetJsonVersionInJar() {
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
            Log(ex, "没有访问实例文件夹的权限");
            return false;
        }

        try {
            _ = VersionJson;
        } catch (Exception ex) {
            Log(ex, $"实例 JSON 可用性检查失败（{Path}）");
            JsonText = "";
            VersionJson = null;
            Info = ex.Message;
            State = McInstanceState.Error;
            return false;
        }

        try {
            if (!string.IsNullOrEmpty(InheritInstance)) {
                if (!File.Exists(GetPathFromFullPath(Path) + InheritInstance + "\\" + InheritInstance + ".json")) {
                    State = McInstanceState.Error;
                    Info = $"需要安装 {InheritInstance} 作为前置实例";
                    return false;
                }
            }
        } catch (Exception ex) {
            Log(ex, $"依赖实例检查出错（{Name}）");
            State = McInstanceState.Error;
            Info = $"未知错误：{ex}";
            return false;
        }

        State = McInstanceState.Original;
        return true;
    }

    public McInstance Load() {
        try {
            if (!Check()) return this;

            // 确定实例分类
            switch (Version.McName) {
                case "Unknown":
                    State = McInstanceState.Error;
                    break;
                case "Old":
                    State = McInstanceState.Old;
                    break;
                default:
                    ProcessApiType();
                    break;
            }

            // 确定实例图标
            Logo = Setup.Instance.LogoPath(Path);
            if (string.IsNullOrEmpty(Logo) || !Setup.Instance.IsLogoCustom(Path)) {
                Logo = State switch {
                    McInstanceState.Original => PathImage + "Blocks/Grass.png",
                    McInstanceState.Snapshot => PathImage + "Blocks/CommandBlock.png",
                    McInstanceState.Old => PathImage + "Blocks/CobbleStone.png",
                    McInstanceState.Forge => PathImage + "Blocks/Anvil.png",
                    McInstanceState.NeoForge => PathImage + "Blocks/NeoForge.png",
                    McInstanceState.Cleanroom => PathImage + "Blocks/Cleanroom.png",
                    McInstanceState.Fabric => PathImage + "Blocks/Fabric.png",
                    McInstanceState.LegacyFabric => PathImage + "Blocks/Fabric.png",
                    McInstanceState.Quilt => PathImage + "Blocks/Quilt.png",
                    McInstanceState.OptiFine => PathImage + "Blocks/GrassPath.png",
                    McInstanceState.LiteLoader => PathImage + "Blocks/Egg.png",
                    McInstanceState.Fool => PathImage + "Blocks/GoldBlock.png",
                    McInstanceState.LabyMod => PathImage + "Blocks/LabyMod.png",
                    _ => PathImage + "Blocks/RedstoneBlock.png"
                };
            }

            // 确定实例描述和状态
            Info = string.IsNullOrEmpty(Setup.Instance.CustomInfo(Path))
                ? GetDefaultDescription()
                : Setup.Instance.CustomInfo(Path);
            IsStar = Setup.Instance.Starred(Path);
            DisplayType = Setup.Instance.DisplayType(Path);

            // 写入缓存
            if (Directory.Exists(Path)) {
                Setup.Instance.State(Path) = State;
                Setup.Instance.Info(Path) = Info;
                Setup.Instance.LogoPath(Path) = Logo;
            }

            if (State != McInstanceState.Error) {
                Setup.Instance.ReleaseTime(Path) = ReleaseTime.ToString("yyyy-MM-dd HH:mm");
                Setup.Instance.FabricVersion(Path) = Version.FabricVersion;
                Setup.Instance.LegacyFabricVersion(Path) = Version.LegacyFabricVersion;
                Setup.Instance.QuiltVersion(Path) = Version.QuiltVersion;
                Setup.Instance.LabyModVersion(Path) = Version.LabyModVersion;
                Setup.Instance.OptiFineVersion(Path) = Version.OptiFineVersion;
                Setup.Instance.HasLiteLoader(Path) = Version.HasLiteLoader;
                Setup.Instance.ForgeVersion(Path) = Version.ForgeVersion;
                Setup.Instance.NeoForgeVersion(Path) = Version.NeoForgeVersion;
                Setup.Instance.CleanroomVersion(Path) = Version.CleanroomVersion;
                Setup.Instance.SortCode(Path) = Version.SortCode;
                Setup.Instance.McVersion(Path) = Version.McName;
                Setup.Instance.VersionMajor(Path) = Version.McCodeMain;
                Setup.Instance.VersionMinor(Path) = Version.McCodeSub;
            }
        } catch (Exception ex) {
            Info = $"未知错误：{ex}";
            Logo = PathImage + "Blocks/RedstoneBlock.png";
            State = McInstanceState.Error;
            Log(ex, $"加载实例失败（{Name}）", LogLevel.Feedback);
        } finally {
            IsLoaded = true;
        }
        return this;
    }

    private void ProcessApiType() {
        string realJson = VersionJson?.ToString() ?? JsonText;
        if (VersionJson?["type"]?.ToString() == "fool" || !string.IsNullOrEmpty(GetMcFoolName(Version.McName))) {
            State = McInstanceState.Fool;
        } else if (Version.McName.Contains("w", StringComparison.OrdinalIgnoreCase) ||
                   Name.Contains("combat", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("rc", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("pre", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("experimental", StringComparison.OrdinalIgnoreCase) ||
                   VersionJson?["type"]?.ToString() is "snapshot" or "pending") {
            State = McInstanceState.Snapshot;
        }

        if (realJson.Contains("optifine")) {
            State = McInstanceState.OptiFine;
            Version.HasOptiFine = true;
            Version.OptiFineVersion = Regex.Match(realJson, "(?<=HD_U_)[^\"":/]+")?.Value ?? "未知版本";
        }
        // ... 其他 API 类型处理类似
    }

    private bool TryGetReleaseTimeAndVersion(out DateTime releaseTime) {
        try {
            releaseTime = VersionJson["releaseTime"]?.ToObject<DateTime>() ?? new DateTime(1970, 1, 1, 15, 0, 0);
            return true;
        } catch {
            releaseTime = new DateTime(1970, 1, 1, 15, 0, 0);
            return false;
        }
    }

    private string GetVersionFromJson() {
        // 实现从 JSON 获取版本号的逻辑
        // 这里需要根据实际需求实现具体逻辑
        return "Unknown";
    }

    private bool IsValidJson(string? json) => !string.IsNullOrWhiteSpace(json) && json.Trim().StartsWith("{") && json.Trim().EndsWith("}");

    private JObject MergeHmclJson(JObject jsonObject) {
        // 实现 HMCL JSON 合并逻辑
        return jsonObject;
    }
}

public enum McInstanceState {
    Error,
    Original,
    Snapshot,
    Fool,
    OptiFine,
    Old,
    Forge,
    NeoForge,
    LiteLoader,
    Fabric,
    LegacyFabric,
    Quilt,
    Cleanroom,
    LabyMod
}

public enum McInstanceCardType {
    Auto,
    Hidden,
    // 其他分类
}
