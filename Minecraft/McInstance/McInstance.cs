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
    public async Task<string> GetPathIndie() => await McInstanceUtils.GetIsolatedPathAsync(this);

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
    /// 是否可安装 Mod
    /// </summary>
    public async Task<bool> GetIsModable() => await McInstanceUtils.GetIsModableAsync(this);

    /// <summary>
    /// 实例信息
    /// </summary>
    public async Task<McInstanceInfo> GetVersionInfoAsync() {
        if (_versionInfo == null) {
            _versionInfo = new McInstanceInfo();
            try {
                // 获取发布时间并判断是否为老版本
                var releaseTime = await McInstanceUtils.TryGetReleaseTimeAsync(this);
                var version = await GetVersionFromJson(this);
                if (version != null) {
                    _versionInfo.McVersion = version;
                    _versionInfo.CanDetermineVersion = true;
                } else {
                    _versionInfo.CanDetermineVersion = false;
                }
                _versionInfo.ReleaseTime = releaseTime;
            } catch (Exception ex) {
                LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
                _versionInfo.McVersion = "Unknown";
                Desc = $"无法识别：{ex.Message}";
            }
        }
        return _versionInfo;
    }

    /// <summary>
    /// 异步获取 JSON 对象。
    /// </summary>
    /// <returns>表示 Minecraft 实例的 JSON 对象。</returns>
    /// <exception cref="Exception">如果初始化 JSON 失败或版本依赖项出现嵌套，则抛出异常。</exception>
    public async Task<JsonObject> GetVersionJsonAsync() {
        if (_versionJson == null) {
            string jsonPath = System.IO.Path.Combine(Path, $"{Name}.json");
            if (!File.Exists(jsonPath)) {
                string[] jsonFiles = Directory.GetFiles(Path, "*.json");
                if (jsonFiles.Length == 1) {
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


    public async Task<string?> GetVersionFromJson(McInstance instance) {
        var versionJson = await instance.GetVersionJsonAsync();
        try {
            string? version = null;
            // Get version from clientVersion
            if (versionJson.TryGetPropertyValue("clientVersion", out var clientVersionElement)) {
                version = clientVersionElement.ToString();
            }

            // Get version from patches
            if (versionJson.TryGetPropertyValue("patches", out var patchesElement) &&
                patchesElement.GetValueKind() == JsonValueKind.Array) {
                var patchesArray = patchesElement as JsonArray;
                foreach (var patch in patchesArray.AsEnumerable()) {
                    if (patch is JsonObject patchObj) {
                        if (patchObj.TryGetPropertyValue("id", out var idElement) && idElement.ToString() == "game" &&
                            patchObj.TryGetPropertyValue("version", out var versionElement)) {
                            version = versionElement.ToString();
                        }
                    }
                }
            }

            // Fallback
            LogWrapper.Warn($"无法完全确认 MC 版本号的实例：{Name}");
            Desc = "PCL 无法识别该实例的 MC 版本号";
            return version;
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            Desc = $"无法识别：{ex.Message}";
            return null;
        }
    }

    public bool IsLoaded { get; private set; }

    public bool IsError { get; private set; }

    public McInstance(string path) {
        Path = (path.Contains(":") ? "" : McFolderManager.PathMcFolder + "versions\\") + path + (path.EndsWith("\\") ? "" : "\\");
    }

    public async Task<bool> Check() {
        if (!Directory.Exists(Path)) {
            IsError = true;
            Desc = $"未找到实例 {Name}";
            return false;
        }

        try {
            Directory.CreateDirectory(Path + "PCL\\");
            await Directories.CheckPermissionWithExceptionAsync(Path + "PCL\\");
        } catch (Exception ex) {
            IsError = true;
            Desc = "PCL 没有对该文件夹的访问权限，请以管理员身份运行";
            LogWrapper.Warn(ex, "没有访问实例文件夹的权限");
            return false;
        }

        try {
            _ = await GetVersionJsonAsync();
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"实例 JSON 可用性检查失败（{Path}）");
            _versionJson = null;
            Desc = ex.Message;
            IsError = true;
            return false;
        }

        return true;
    }

    public async Task<McInstance> Load() {
        try {
            var versionInfo = await GetVersionInfoAsync();
            await Check();

            // 确定实例图标
            Logo = await DetermineLogo();

            // 确定实例描述和状态
            Desc = string.IsNullOrEmpty(SetupService.GetString(SetupEntries.Instance.CustomInfo, Path))
                ? await McInstanceUtils.GetDefaultDescription(this)
                : SetupService.GetString(SetupEntries.Instance.CustomInfo, Path);
            SetupService.SetString(SetupEntries.Instance.Starred, Path);
            IsFavorited = SetupService.GetBool(SetupEntries.Instance.Starred, Path);
            DisplayType = (McInstanceCardType)SetupService.GetInt32(SetupEntries.Instance.DisplayType, Path);

            // 写入缓存
            if (Directory.Exists(Path)) {
                SetupService.SetString(SetupEntries.Instance.Info, Desc, Path);
                SetupService.SetString(SetupEntries.Instance.LogoPath, Logo, Path);
            }

            if (!IsError) {
                SetupService.SetString(SetupEntries.Instance.ReleaseTime, versionInfo.ReleaseTime.ToString("yyyy-MM-dd HH:mm"), Path);
                SetupService.SetString(SetupEntries.Instance.FabricVersion, versionInfo.FabricVersion, Path);
                SetupService.SetString(SetupEntries.Instance.LegacyFabricVersion, versionInfo.LegacyFabricVersion, Path);
                SetupService.SetString(SetupEntries.Instance.QuiltVersion, versionInfo.QuiltVersion, Path);
                SetupService.SetString(SetupEntries.Instance.LabyModVersion, versionInfo.LabyModVersion, Path);
                SetupService.SetString(SetupEntries.Instance.OptiFineVersion, versionInfo.OptiFineVersion, Path);
                SetupService.SetBool(SetupEntries.Instance.HasLiteLoader, versionInfo.HasLiteLoader, Path);
                SetupService.SetString(SetupEntries.Instance.ForgeVersion, versionInfo.ForgeVersion, Path);
                SetupService.SetString(SetupEntries.Instance.NeoForgeVersion, versionInfo.NeoForgeVersion, Path);
                SetupService.SetString(SetupEntries.Instance.CleanroomVersion, versionInfo.CleanroomVersion, Path);
                SetupService.SetInt32(SetupEntries.Instance.SortCode, versionInfo.SortCode, Path);
                SetupService.SetString(SetupEntries.Instance.McVersion, versionInfo.McVersion, Path);
                SetupService.SetInt32(SetupEntries.Instance.VersionMajor, versionInfo.McCodeMain, Path);
                SetupService.SetInt32(SetupEntries.Instance.VersionMinor, versionInfo.McCodeSub, Path);
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

    private async Task<string> DetermineLogo() {
        var logo = SetupService.GetString(SetupEntries.Instance.LogoPath, Path);
        var versionInfo = await GetVersionInfoAsync();
        if (string.IsNullOrEmpty(logo) || !SetupService.GetBool(SetupEntries.Instance.IsLogoCustom, Path)) {
            if (IsError) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
            }

            // 优先判断特殊版本
            if (versionInfo.IsFoolVersion) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/GoldBlock.png");
            }
            if (versionInfo.IsOldVersion) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/CobbleStone.png");
            }
            if (versionInfo.IsSnapshotVersion) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/CommandBlock.png");
            }

            // 其次判断加载器等
            if (versionInfo.HasNeoForge) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/NeoForge.png");
            }
            if (versionInfo.HasFabric || versionInfo.HasLegacyFabric) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Fabric.png");
            }
            if (versionInfo.HasForge) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Forge.png");
            }
            if (versionInfo.HasLiteLoader) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Egg.png");
            }
            if (versionInfo.HasQuilt) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Quilt.png");
            }
            if (versionInfo.HasCleanroom) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Cleanroom.png");
            }
            if (versionInfo.HasLabyMod) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/LabyMod.png");
            }
            if (versionInfo.HasOptiFine) {
                return System.IO.Path.Combine(Basics.ImagePath, "Blocks/OptiFine.png");
            }

            // 正常版本
            return System.IO.Path.Combine(Basics.ImagePath, "Blocks/Grass.png");
        }
        return logo;
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
