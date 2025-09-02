using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
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
    private McInstanceInfo? _versionInfo;
    private JsonObject? _versionJsonInJar;

    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 应用版本隔离后的 Minecraft 根文件夹路径，以“\”结尾
    /// </summary>
    public async Task<string> GetIsolatedPath() => await McInstanceLogic.GetIsolatedPathAsync(this);

    /// <summary>
    /// 实例文件夹名称
    /// </summary>
    public string Name => _name ??= string.IsNullOrEmpty(Path) ? "" : new DirectoryInfo(Path).Name;

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

    /// <summary>
    /// 强制实例分类
    /// </summary>
    public McInstanceCardType DisplayType { get; set; } = McInstanceCardType.Auto;

    /// <summary>
    /// 实例信息
    /// </summary>
    public async Task<McInstanceInfo> GetVersionInfoAsync() {
        if (_versionInfo != null) {
            return _versionInfo;
        }

        _versionInfo = new McInstanceInfo();
        var versionJson = await GetVersionJsonAsync();
        
        // 获取 MC 版本
        var version = await McInstanceLogic.GetVersionFromJson(this);
        
        if (version != null) {
            _versionInfo.McVersion = version;
        } else {
            LogWrapper.Warn("识别 Minecraft 版本时出错");
            _versionInfo.McVersion = "Unknown";
            Desc = "无法识别 Minecraft 版本";
        }
        
        // 获取发布时间
        var releaseTime = await McInstanceLogic.GetReleaseTime(this);
        _versionInfo.ReleaseTime = releaseTime;
        
        // 获取版本类型
        _versionInfo.VersionType = await McInstanceLogic.GetVersionType(this, releaseTime);

        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
        try {
            if (IsPatchesFormatJson) {
                foreach (var patch in versionJson["patches"]!.AsArray()) {
                    var patcherInfo = patch.Deserialize<PatcherInfo>(options);
                    _versionInfo.Patchers.Add(patcherInfo);
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            _versionInfo.McVersion = "Unknown";
            Desc = $"无法识别：{ex.Message}";
        }

        return _versionInfo;
    }

    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    /// <exception cref="Exception">如果初始化 JSON 失败或版本依赖项出现嵌套，则抛出异常。</exception>
    public async Task<JsonObject> GetVersionJsonAsync() {
        if (_versionJson != null) {
            return _versionJson;
        }

        var jsonPath = System.IO.Path.Combine(Path, $"{Name}.json");
        if (!File.Exists(jsonPath)) {
            var jsonFiles = Directory.GetFiles(Path, "*.json");
            if (jsonFiles.Length == 1) {
                jsonPath = jsonFiles[0];
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
    public async Task<bool> GetIsOldJsonAsync() => (await GetVersionJsonAsync())["minecraftArguments"]?.ToString() != null;

    /// <summary>
    /// 是否为 Patches 格式 JSON
    /// </summary>
    public bool IsPatchesFormatJson { get; set; }

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

    public bool IsLoaded { get; private set; }

    public bool IsError { get; private set; }

    public McInstance(string path) {
        Path = (path.Contains(":") ? "" : McFolderManager.PathMcFolder + "versions\\") + path + (path.EndsWith("\\") ? "" : "\\");
    }

    private async Task<bool> Check() {
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

        try {
            await GetVersionJsonAsync();
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"实例 JSON 可用性检查失败（{Path}）");
            Desc = ex.Message;
            return false;
        }

        return true;
    }

    public async Task<McInstance> Load() {
        try {
            if (!await Check()) {
                IsError = true;
            }

            // 确定实例图标
            Logo = await McInstanceLogic.DetermineLogo(this);

            // 确定实例描述和状态
            Desc = string.IsNullOrEmpty(SetupService.GetString(SetupEntries.Instance.CustomInfo, Path))
                ? await McInstanceLogic.GetDefaultDescription(this)
                : SetupService.GetString(SetupEntries.Instance.CustomInfo, Path);
            SetupService.SetString(SetupEntries.Instance.Starred, Path);
            IsFavorited = SetupService.GetBool(SetupEntries.Instance.Starred, Path);
            DisplayType = (McInstanceCardType)SetupService.GetInt32(SetupEntries.Instance.DisplayType, Path);

            // 写入缓存
            if (Directory.Exists(Path)) {
                SetupService.SetString(SetupEntries.Instance.Info, Desc, Path);
                SetupService.SetString(SetupEntries.Instance.LogoPath, Logo, Path);
            }
        } catch (Exception ex) {
            Desc = $"未知错误：{ex}";
            Logo = Basics.ImagePath + "Blocks/RedstoneBlock.png";
            IsError = true;
            LogWrapper.Warn(ex, $"加载实例失败（{Name}）");
        } finally {
            IsLoaded = true;
        }
        return this;
    }
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
