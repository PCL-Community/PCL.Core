using System;
using System.Text.Json;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance;
using PCL.Core.UI;

namespace PCL.Core.Minecraft.Launch;

public class LaunchArgumentBuilder {
    private readonly McNoPatchesInstance _noPatchesInstance;
    private readonly JavaInfo _selectedJavaInfo;
    private readonly McLoginResult _loginResult;
        
    public LaunchArgumentBuilder(McNoPatchesInstance noPatchesInstance, JavaInfo selectedJavaInfo, McLoginResult loginResult)
    {
        _noPatchesInstance = noPatchesInstance;
        _selectedJavaInfo = selectedJavaInfo;
        _loginResult = loginResult;
    }
    
    public LaunchArgumentBuilder AddJvmArguments()
    {
        // JVM参数构建逻辑
        return this;
    }
    
    public LaunchArgumentBuilder AddGameArguments()
    {
        // Game参数构建逻辑
        return this;
    }
    
    public string Build()
    {
        // 最终参数组装
        return "";
    }
    
    private void McLaunchArgumentMain(string a) {
        if (McInstanceManager.Current == null) return;
        
        McLaunchUtils.Log("开始获取 Minecraft 启动参数");
        // 获取基准字符串与参数信息
        var arguments = "";
        if (McInstanceManager.Current._versionJson!.TryGetPropertyValue("arguments", out var argumentNode) &&
            argumentNode!.GetValueKind() == JsonValueKind.Object &&
            argumentNode.AsObject().TryGetPropertyValue("jvm", out var jvmNode)) {
            McLaunchUtils.Log("获取新版 JVM 参数");
            arguments = McLaunchArgumentsJvmNew(McInstanceManager.Current);
            McLaunchUtils.Log("新版 JVM 参数获取成功：");
            McLaunchUtils.Log(arguments);
        } else {
            McLaunchUtils.Log("获取旧版 JVM 参数");
            arguments = McLaunchArgumentsJvmOld(McInstanceManager.Current);
            McLaunchUtils.Log("旧版 JVM 参数获取成功：");
            McLaunchUtils.Log(arguments);
        }

        if (!string.IsNullOrEmpty(McInstanceManager.Current._versionJson["minecraftArguments"]?.ToString())) {
            McLaunchUtils.Log("获取旧版 Game 参数");
            arguments += " " + McLaunchArgumentsGameOld(McInstanceManager.Current);
            McLaunchUtils.Log("旧版 Game 参数获取成功");
        }

        if (McInstanceManager.Current._versionJson!.TryGetPropertyValue("arguments", out var argumentNode2) &&
            argumentNode2!.GetValueKind() == JsonValueKind.Object &&
            argumentNode2.AsObject().TryGetPropertyValue("game", out var gameNode)) {
            McLaunchUtils.Log("获取新版 Game 参数");
            arguments += " " + McLaunchArgumentsGameNew(McInstanceManager.Current);
            McLaunchUtils.Log("新版 Game 参数获取成功");
        }

        // 编码参数
        if (McLaunchJavaSelected.JavaMajorVersion > 8) {
            if (!arguments.Contains("-Dstdout.encoding=")) arguments = "-Dstdout.encoding=UTF-8 " + arguments;
            if (!arguments.Contains("-Dstderr.encoding=")) arguments = "-Dstderr.encoding=UTF-8 " + arguments;
        }
        if (McLaunchJavaSelected.JavaMajorVersion >= 18) {
            if (!arguments.Contains("-Dfile.encoding=")) arguments = "-Dfile.encoding=COMPAT " + arguments;
        }

        // MJSB
        arguments = arguments.Replace(" -Dos.name=Windows 10", " -Dos.name=\"Windows 10\"");

        // 全屏
        if (Config.Launch.WindowType == 0) arguments += " --fullscreen";

        // 由 Option 传入的额外参数
        foreach (var arg in CurrentLaunchOptions.ExtraArgs) {
            arguments += " " + arg.Trim();
        }

        // 自定义参数
        var argumentGame = Config.Instance.GameArgs[McInstanceManager.Current.Path];
        arguments += " " + (string.IsNullOrEmpty(argumentGame) ? Setup.Get("LaunchAdvanceGame") : argumentGame);

        // 替换参数
        var replaceArguments = McLaunchArgumentsReplace(McInstanceManager.Current, loader);
        if (string.IsNullOrWhiteSpace(replaceArguments["${version_type}"])) {
            arguments = arguments.Replace(" --versionType ${version_type}", "");
            replaceArguments["${version_type}"] = "\"\"";
        }

        var finalArguments = "";
        foreach (var argument in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
            var tempArg = argument;
            foreach (var entry in replaceArguments) {
                tempArg = tempArg.Replace(entry.Key, entry.Value);
            }
            if ((tempArg.Contains(" ") || tempArg.Contains(":\"")) && !tempArg.EndsWith("\"")) {
                tempArg = $"\"{tempArg}\"";
            }
            finalArguments += tempArg + " ";
        }
        finalArguments = finalArguments.TrimEnd();

        // 进存档
        string worldName = CurrentLaunchOptions.WorldName;
        if (!string.IsNullOrEmpty(worldName)) {
            finalArguments += $" --quickPlaySingleplayer \"{worldName}\"";
        }

        // 进服
        string server = string.IsNullOrEmpty(CurrentLaunchOptions.ServerIp)
            ? Config.Instance.ServerToEnter[McInstanceManager.Current.Path]
            : CurrentLaunchOptions.ServerIp;
        if (string.IsNullOrWhiteSpace(worldName) && !string.IsNullOrWhiteSpace(server)) {
            if (McInstanceManager.Current.ReleaseTime > new DateTime(2023, 4, 4)) {
                finalArguments += $" --quickPlayMultiplayer \"{server}\"";
            } else {
                if (server.Contains(":")) {
                    var parts = server.Split(':');
                    finalArguments += $" --server {parts[0]} --port {parts[1]}";
                } else {
                    finalArguments += $" --server {server} --port 25565";
                }
                if (McInstanceManager.Current.HasPatcher("OptiFine")) {
                    HintWrapper.Show("OptiFine 与自动进入服务器可能不兼容，有概率导致材质丢失甚至游戏崩溃！", HintTheme.Error);
                }
            }
        }

        // 输出
        McLaunchUtils.Log("Minecraft 启动参数：");
        McLaunchUtils.Log(finalArguments);
        McLaunchArgument = finalArguments;
    }
}
