using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public class McInstance {
    private readonly McInstanceIsolationHandler _isolationHandler;
    
    private string? _name;
    private string? _jsonText;
    private JObject? _jsonObject;
    private McInstanceInfo? _version;
    private string? _inheritVersion;
    private JObject? _jsonVersion;
    private bool _jsonVersionInited;

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
    public string Name => _name ??= string.IsNullOrEmpty(Path) ? "" : GetFolderNameFromPath(Path);

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
                    Log(ex, "识别 Minecraft 版本时出错");
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
    /// JSON 文本
    /// </summary>
    public string JsonText {
        get {
            if (_jsonText == null) {
                string jsonPath = $"{Path}{Name}.json";
                if (!File.Exists(jsonPath)) {
                    var jsonFiles = Directory.GetFiles(Path, "*.json");
                    if (jsonFiles.Length == 1) {
                        jsonPath = jsonFiles[0];
                        LogWrapper.Debug($"[Minecraft] 未找到同名实例 JSON，自动换用 {jsonPath}");
                    } else {
                        throw new Exception($"未找到实例 JSON 文件：{jsonPath}");
                    }
                }
                
                _jsonText = File.ReadAllText(jsonPath);
            }
            return _jsonText;
        }
        set => _jsonText = value;
    }

    /// <summary>
    /// JSON 对象
    /// </summary>
    public JObject JsonObject {
        get {
            if (_jsonObject == null) {
                string text = JsonText;
                try {
                    _jsonObject = GetJson(text);
                    if (_jsonObject.ContainsKey("patches") && !_jsonObject.ContainsKey("time")) {
                        IsHmclFormatJson = true;
                        _jsonObject = MergeHmclJson(_jsonObject);
                        _jsonObject["id"] = Name;
                        _jsonObject.Remove("inheritsFrom");
                    }

                    string inheritInstance = _jsonObject["inheritsFrom"]?.ToString() ?? "";
                    while (inheritInstance != "" && inheritInstance != Name) {
                        var inherit = new McInstance(inheritInstance);
                        if (inherit.InheritInstance == inheritInstance) {
                            throw new Exception($"版本依赖项出现嵌套：{inheritInstance}");
                        }
                        inherit.JsonObject.Merge(_jsonObject);
                        _jsonObject = inherit.JsonObject;
                        inheritInstance = inherit.InheritInstance;
                    }
                } catch (Exception ex) {
                    throw new Exception($"初始化实例 JSON 失败（{Name ?? "null"}）", ex);
                }
            }
            return _jsonObject;
        }
        set => _jsonObject = value;
    }

    /// <summary>
    /// 是否为旧版 JSON 格式
    /// </summary>
    public bool IsOldJson => JsonObject["minecraftArguments"]?.ToString() != null;

    /// <summary>
    /// 是否为 HMCL 格式 JSON
    /// </summary>
    public bool IsHmclFormatJson { get; set; }

    /// <summary>
    /// 实例 JAR 中的 version.json 文件对象
    /// </summary>
    public JObject? JsonVersion {
        get {
            if (!_jsonVersionInited) {
                _jsonVersionInited = true;
                string jarPath = $"{Path}{Name}.jar";
                if (File.Exists(jarPath)) {
                    try {
                        using var jarArchive = new ZipArchive(new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        var versionJson = jarArchive.GetEntry("version.json");
                        if (versionJson != null) {
                            using var reader = new StreamReader(versionJson.Open());
                            _jsonVersion = GetJson(reader.ReadToEnd());
                        }
                    } catch (Exception ex) {
                        Log(ex, "从实例 JAR 中读取 version.json 失败");
                    }
                }
            }
            return _jsonVersion;
        }
    }

    /// <summary>
    /// 依赖实例名称，若无则为空字符串
    /// </summary>
    public string InheritInstance {
        get {
            if (_inheritVersion == null) {
                _inheritVersion = JsonObject["inheritsFrom"]?.ToString() ?? "";
                if (JsonText.Contains("liteloader") && Version.McName != Name && !JsonText.Contains("logging")) {
                    if (JsonObject["jar"]?.ToString() == Version.McName) {
                        _inheritVersion = Version.McName;
                    }
                }
                if (IsHmclFormatJson) _inheritVersion = "";
            }
            return _inheritVersion;
        }
    }

    public bool IsLoaded { get; private set; }

    public McInstance(string path) {
        Path = (path.Contains(":") ? "" : PathMcFolder + "versions\\") + path + (path.EndsWith("\\") ? "" : "\\");
    }

    public bool Check() {
        if (!Directory.Exists(Path)) {
            State = McInstanceState.Error;
            Info = $"未找到实例 {Name}";
            return false;
        }

        try {
            Directory.CreateDirectory(Path + "PCL\\");
            CheckPermissionWithException(Path + "PCL\\");
        } catch (Exception ex) {
            State = McInstanceState.Error;
            Info = "PCL 没有对该文件夹的访问权限，请以管理员身份运行";
            Log(ex, "没有访问实例文件夹的权限");
            return false;
        }

        try {
            _ = JsonObject;
        } catch (Exception ex) {
            Log(ex, $"实例 JSON 可用性检查失败（{Path}）");
            JsonText = "";
            JsonObject = null;
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
            Logo = NEWSetup.Instance.LogoPath(Path);
            if (string.IsNullOrEmpty(Logo) || !NEWSetup.Instance.IsLogoCustom(Path)) {
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
            Info = string.IsNullOrEmpty(NEWSetup.Instance.CustomInfo(Path))
                ? GetDefaultDescription()
                : NEWSetup.Instance.CustomInfo(Path);
            IsStar = NEWSetup.Instance.Starred(Path);
            DisplayType = NEWSetup.Instance.DisplayType(Path);

            // 写入缓存
            if (Directory.Exists(Path)) {
                NEWSetup.Instance.State(Path) = State;
                NEWSetup.Instance.Info(Path) = Info;
                NEWSetup.Instance.LogoPath(Path) = Logo;
            }

            if (State != McInstanceState.Error) {
                NEWSetup.Instance.ReleaseTime(Path) = ReleaseTime.ToString("yyyy-MM-dd HH:mm");
                NEWSetup.Instance.FabricVersion(Path) = Version.FabricVersion;
                NEWSetup.Instance.LegacyFabricVersion(Path) = Version.LegacyFabricVersion;
                NEWSetup.Instance.QuiltVersion(Path) = Version.QuiltVersion;
                NEWSetup.Instance.LabyModVersion(Path) = Version.LabyModVersion;
                NEWSetup.Instance.OptiFineVersion(Path) = Version.OptiFineVersion;
                NEWSetup.Instance.HasLiteLoader(Path) = Version.HasLiteLoader;
                NEWSetup.Instance.ForgeVersion(Path) = Version.ForgeVersion;
                NEWSetup.Instance.NeoForgeVersion(Path) = Version.NeoForgeVersion;
                NEWSetup.Instance.CleanroomVersion(Path) = Version.CleanroomVersion;
                NEWSetup.Instance.SortCode(Path) = Version.SortCode;
                NEWSetup.Instance.McVersion(Path) = Version.McName;
                NEWSetup.Instance.VersionMajor(Path) = Version.McCodeMain;
                NEWSetup.Instance.VersionMinor(Path) = Version.McCodeSub;
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
        string realJson = JsonObject?.ToString() ?? JsonText;
        if (JsonObject?["type"]?.ToString() == "fool" || !string.IsNullOrEmpty(GetMcFoolName(Version.McName))) {
            State = McInstanceState.Fool;
        } else if (Version.McName.Contains("w", StringComparison.OrdinalIgnoreCase) ||
                   Name.Contains("combat", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("rc", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("pre", StringComparison.OrdinalIgnoreCase) ||
                   Version.McName.Contains("experimental", StringComparison.OrdinalIgnoreCase) ||
                   JsonObject?["type"]?.ToString() is "snapshot" or "pending") {
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
            releaseTime = JsonObject["releaseTime"]?.ToObject<DateTime>() ?? new DateTime(1970, 1, 1, 15, 0, 0);
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

public class McInstanceInfo {
    // 实现类似 VB.NET 中的 McInstanceInfo 类
    // 包含原版信息、API 信息等
}

public enum McInstanceCardType {
    Auto,
    Hidden,
    // 其他分类
}
