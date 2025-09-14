using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Folder;
using PCL.Core.Minecraft.Instance;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Launch.Utils;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Launch.Services.Argument;

public class LaunchArgBuilder(IMcInstance instance, JavaInfo selectedJava, bool isDemo) {
    private readonly List<string> _arguments = [];

    private readonly IJsonBasedInstance _jsonBasedInstance = (IJsonBasedInstance)instance;

    /// <summary>
    /// 构建基础的 JVM 参数（必备）
    /// </summary>
    public LaunchArgBuilder AddJvmArguments() {
        var jvmArgBuilder = new JvmArgBuilder(instance);

        if (_jsonBasedInstance.VersionJson!.TryGetPropertyValue("arguments", out var argumentNode) &&
            argumentNode!.GetValueKind() == JsonValueKind.Object &&
            argumentNode.AsObject().TryGetPropertyValue("jvm", out var jvmNode)) {
            McLaunchUtils.Log("获取新版 JVM 参数");
            _arguments.AddRange(jvmArgBuilder.BuildLegacyJvmArguments(selectedJava));
            McLaunchUtils.Log("新版 JVM 参数获取成功");
        } else {
            McLaunchUtils.Log("获取旧版 JVM 参数");
            _arguments.AddRange(jvmArgBuilder.BuildModernJvmArguments(selectedJava));
            McLaunchUtils.Log("旧版 JVM 参数获取成功");
        }
        return this;
    }

    public LaunchArgBuilder AddGameArguments() {
        var gameArgBuilder = new GameArgBuilder(instance);

        if (!string.IsNullOrEmpty(_jsonBasedInstance.VersionJson!["minecraftArguments"]?.ToString())) {
            McLaunchUtils.Log("获取旧版 Game 参数");
            _arguments.AddRange(gameArgBuilder.BuildLegacyGameArguments());
            McLaunchUtils.Log("旧版 Game 参数获取成功");
        }

        if (_jsonBasedInstance.VersionJson!.TryGetPropertyValue("arguments", out var argumentNode2) &&
            argumentNode2!.GetValueKind() == JsonValueKind.Object &&
            argumentNode2.AsObject().TryGetPropertyValue("game", out _)) {
            McLaunchUtils.Log("获取新版 Game 参数");
            _arguments.AddRange(gameArgBuilder.BuildModernGameArguments());
            McLaunchUtils.Log("新版 Game 参数获取成功");
        }
        // Game参数构建逻辑
        return this;
    }

    public LaunchArgBuilder AddWorldArguments(string? worldName = null, string? serverIp = null) {
        // 进存档
        if (!string.IsNullOrEmpty(worldName)) {
            _arguments.Add($"--quickPlaySingleplayer \"{worldName}\"");
        }
        
        // 进服
        var server = string.IsNullOrEmpty(serverIp)
            ? Config.Instance.ServerToEnter[instance.Path]
            : serverIp;
        
        if (string.IsNullOrWhiteSpace(worldName) && !string.IsNullOrWhiteSpace(server)) {
            if (instance.InstanceInfo.ReleaseTime > new DateTime(2023, 4, 4)) {
                _arguments.Add($"--quickPlayMultiplayer \"{server}\"");
            } else {
                if (server.Contains(':')) {
                    var parts = server.Split(':');
                    _arguments.Add($"--server {parts[0]} --port {parts[1]}");
                } else {
                    _arguments.Add($"--server {server} --port 25565");
                }
                if (instance.InstanceInfo.HasPatch("optifine")) {
                    HintWrapper.Show("OptiFine 与自动进入服务器可能不兼容，有概率导致材质丢失甚至游戏崩溃！", HintTheme.Error);
                }
            }
        }

        return this;
    }

    public LaunchArgBuilder AddOtherArguments() {
        // 编码参数
        if (selectedJava.JavaMajorVersion > 8) {
            if (!_arguments.Contains("-Dstdout.encoding=")) _arguments.Add("-Dstdout.encoding=UTF-8");
            if (!_arguments.Contains("-Dstderr.encoding=")) _arguments.Add("-Dstderr.encoding=UTF-8");
        }
        if (selectedJava.JavaMajorVersion >= 18) {
            if (!_arguments.Contains("-Dfile.encoding=")) _arguments.Add("-Dfile.encoding=COMPAT ");
        }

        // MJSB
        var index = _arguments.IndexOf("-Dos.name=Windows 10");
        if (index != -1) {
            _arguments[index] = "-Dos.name=\"Windows 10\"";
        }

        // 全屏
        if (Config.Launch.WindowType == 0) _arguments.Add("--fullscreen");

        // 由 Option 传入的额外参数
        if (isDemo) {
            _arguments.Add("--demo");
        }

        // 自定义参数
        var argumentGame = Config.Instance.GameArgs[instance.Path];
        _arguments.Add(string.IsNullOrEmpty(argumentGame) ? Config.Launch.GameArgs : argumentGame);

        // 替换参数
        var replaceArguments = await McLaunchArgumentsReplace();
        if (string.IsNullOrWhiteSpace(replaceArguments["${version_type}"])) {
            _arguments = _arguments.Replace(" --versionType ${version_type}", "");
            replaceArguments["${version_type}"] = "\"\"";
        }

        var finalArguments = "";
        foreach (var argument in string.Join(' ', _arguments).Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            var tempArg = argument;
            foreach (var (key, value) in replaceArguments) {
                tempArg = tempArg.Replace(key, value);
            }
            if ((tempArg.Contains(" ") || tempArg.Contains(":\"")) && !tempArg.EndsWith("\"")) {
                tempArg = $"\"{tempArg}\"";
            }
            finalArguments += tempArg + " ";
        }
        finalArguments = finalArguments.TrimEnd();
        return this;
    }
    
    public string Build() {
        var result = new StringBuilder();

        if (jvmArguments.Count > 0) {
            result.Append("-D ");
            result.Append(string.Join(" ", jvmArguments));
        }

        if (otherArguments.Count > 0) {
            if (result.Length > 0) {
                result.Append(" ");
            }
            result.Append(string.Join(" ", otherArguments));
        }

        return result.ToString().Trim();
    }

    private async Task<Dictionary<string, string>> McLaunchArgumentsReplace() {
        var GameArguments = new Dictionary<string, string>();

        // 基础参数
        GameArguments.Add("${classpath_separator}", ";");
        GameArguments.Add("${natives_directory}", GetNativesFolder());
        GameArguments.Add("${library_directory}", Path.Combine(instance.Folder.Path, "libraries"));
        GameArguments.Add("${libraries_directory}", Path.Combine(instance.Folder.Path, "libraries"));
        GameArguments.Add("${launcher_name}", "PCLCE");
        GameArguments.Add("${launcher_version}", "409"); // TODO: 等待迁移
        GameArguments.Add("${version_name}", instance.Name);
        var argumentInfo = Config.Instance.TypeInfo[instance.Path];
        GameArguments.Add("${version_type}", string.IsNullOrEmpty(argumentInfo) ? Config.Launch.TypeInfo : argumentInfo);
        GameArguments.Add("${game_directory}", instance.IsolatedPath.Substring(0, instance.IsolatedPath.Length - 1));
        GameArguments.Add("${assets_root}", FolderService.FolderManager.CurrentFolder + "assets");
        GameArguments.Add("${user_properties}", "{}");
        /*
        GameArguments.Add("${auth_player_name}", McLoginLoader.Output.Name);
        GameArguments.Add("${auth_uuid}", McLoginLoader.Output.Uuid);
        GameArguments.Add("${auth_access_token}", McLoginLoader.Output.AccessToken);
        GameArguments.Add("${access_token}", McLoginLoader.Output.AccessToken);
        GameArguments.Add("${auth_session}", McLoginLoader.Output.AccessToken);
        */
        GameArguments.Add("${user_type}", "msa"); // #1221

        // 窗口尺寸参数
        Size GameSize;
        switch (Config.Launch.WindowType) {
            case 2: // 与启动器尺寸一致
                Size Result = default;
                // RunInUiWait(() => Result = new Size(GetPixelSize(FrmMain.PanForm.ActualWidth), GetPixelSize(FrmMain.PanForm.ActualHeight)));
                GameSize = Result;
                GameSize.Height -= 29.5 * UiHelper.GetSystemDpi() / 96; // 标题栏高度
                break;
            case 3: // 自定义
                GameSize = new Size(Math.Max(100, Config.Launch.WindowWidthLaunch), Math.Max(100, Config.Launch.WindowHeightLaunch));
                break;
            default:
                GameSize = new Size(854, 480);
                break;
        }

        if (instance.InstanceInfo.McVersionBuild <= 12 &&
            selectedJava.JavaMajorVersion <= 8 && selectedJava.Version.Revision >= 200 && selectedJava.Version.Revision <= 321 &&
            !instance.InstanceInfo.HasPatch("optifine") && !instance.InstanceInfo.HasPatch("forge")) {
            // 修复 #3463：1.12.2-，JRE 8u200~321 下窗口大小为设置大小的 DPI% 倍
            McLaunchUtils.Log($"已应用窗口大小过大修复（{selectedJava.Version.Revision}）");
            GameSize.Width /= UiHelper.GetSystemDpi() / 96.0;
            GameSize.Height /= UiHelper.GetSystemDpi() / 96.0;
        }

        GameArguments.Add("${resolution_width}", $"{Math.Round(GameSize.Width)}");
        GameArguments.Add("${resolution_height}", $"{Math.Round(GameSize.Height)}");

        // Assets 相关参数
        GameArguments.Add("${game_assets}", instance.Folder.Path + @"assets\virtual\legacy"); // 1.5.2 的 pre-1.6 资源索引应与 legacy 合并
        GameArguments.Add("${assets_index_name}", McAssetsGetIndexName(instance));

        // 支持库参数
        List<McLibToken> LibList = McLibListGet(instance, true);
        loader.Output = LibList;
        var CpStrings = new List<string>();
        string OptiFineCp = null;

        // RetroWrapper 释放
        if (LaunchEnvUtils.NeedRetroWrapper(instance)) {
            var wrapperPath = Path.Combine(instance.Folder.Path, "libraries/retrowrapper/RetroWrapper.jar");
            try {
                await Files.WriteFileAsync(wrapperPath, Basics.GetResourceStream("Resources/retro-wrapper.jar"));
                CpStrings.Add(wrapperPath);
            } catch (Exception ex) {
                LogWrapper.Warn(ex, "RetroWrapper 释放失败");
            }
        }

        foreach (var library in LibList) {
            if (library.IsNatives) continue;
            if (library.Name != null && library.Name.Contains("com.cleanroommc:cleanroom:0.2")) {
                // Cleanroom 的主 Jar 必须放在 ClassPath 第一位
                CpStrings.Insert(0, library.LocalPath);
            }
            if (library.Name != null && library.Name == "optifine:OptiFine") {
                OptiFineCp = library.LocalPath;
            } else {
                CpStrings.Add(library.LocalPath);
            }
        }
        if (OptiFineCp != null) {
            CpStrings.Insert(CpStrings.Count - 2, OptiFineCp); // OptiFine 总是需要放到倒数第二位
        }
        GameArguments.Add("${classpath}", string.Join(";", CpStrings));

        return GameArguments;
    }

    /// <summary>
    /// 获取某实例资源文件索引名，优先使用 assetIndex，其次使用 assets。失败会返回 legacy。
    /// </summary>
    /// <param name="instance">Minecraft 实例对象，其 JsonObject 属性应为 JsonElement 类型。</param>
    public string McAssetsGetIndexName(IMcInstance instance) {
        try {
            // 使用 TryGetProperty 方法安全地获取属性
            if (_jsonBasedInstance.VersionJson!.TryGetPropertyValue("assetIndex", out var assetIndexElement) && assetIndexElement!.GetValueKind() == JsonValueKind.Object) {
                // 检查 assetIndex 对象中的 "id" 属性
                if (assetIndexElement.AsObject().TryGetPropertyValue("id", out var idElement) && idElement!.GetValueKind() == JsonValueKind.String) {
                    // 获取字符串值并返回
                    return idElement.ToString(); // 使用 ?? 避免 GetString() 返回 null 的情况
                }
            }

            // 检查 "assets" 属性
            if (_jsonBasedInstance.VersionJson.TryGetPropertyValue("assets", out var assetsElement) && assetsElement!.GetValueKind() == JsonValueKind.String) {
                return assetsElement.ToString();
            }

            return "legacy";
        } catch (Exception ex) {
            // 捕获并记录异常
            LogWrapper.Warn(ex, "获取资源文件索引名失败");
        }

        // 默认返回 "legacy"
        return "legacy";
    }

    /// <summary>
    /// 获取 Natives 文件夹路径，不以 \ 结尾。
    /// </summary>
    private string GetNativesFolder() {
        var result = Path.Combine(instance.Path, instance.Name, "-natives");
        if (EncodingUtils.IsDefaultEncodingGbk() || result.IsASCII())
            return result;

        result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "bin", "natives");
        if (result.IsASCII())
            return result;

        return Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL", "natives");
    }
}
