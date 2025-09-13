using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Service;
using PCL.Core.UI;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.Launch.Services.Argument;

public class LaunchArgumentBuilder (IMcInstance noPatchesInstance, JavaInfo selectedJavaInfo) {
    private readonly List<string> jvmArguments;
    private readonly List<string> otherArguments;
    
    public LaunchArgumentBuilder AddJvmArguments() {
        var arguments = "";
        
        var jsonBasedInstance = InstanceManager.Current as IJsonBasedInstance;
        if (jsonBasedInstance!.VersionJson!.TryGetPropertyValue("arguments", out var argumentNode) &&
            argumentNode!.GetValueKind() == JsonValueKind.Object &&
            argumentNode.AsObject().TryGetPropertyValue("jvm", out var jvmNode)) {
            McLaunchUtils.Log("获取新版 JVM 参数");
            arguments = McLaunchArgumentsJvmNew(InstanceManager.Current);
            McLaunchUtils.Log("新版 JVM 参数获取成功：");
            McLaunchUtils.Log(arguments);
        } else {
            McLaunchUtils.Log("获取旧版 JVM 参数");
            arguments = McLaunchArgumentsJvmOld(InstanceManager.Current);
            McLaunchUtils.Log("旧版 JVM 参数获取成功：");
            McLaunchUtils.Log(arguments);
        }
        return this;
    }
    
    public LaunchArgumentBuilder AddGameArguments()
    {
        // Game参数构建逻辑
        return this;
    }
    
    public string Build() {
        StringBuilder result = new StringBuilder();

        if (jvmArguments.Count > 0)
        {
            result.Append("-D ");
            result.Append(string.Join(" ", jvmArguments));
        }

        if (otherArguments.Count > 0)
        {
            if (result.Length > 0)
            {
                result.Append(" ");
            }
            result.Append(string.Join(" ", otherArguments));
        }

        return result.ToString().Trim();
    }
    
    private void McLaunchArgumentMain(string a) {
        if (InstanceManager.Current == null) return;
        
        McLaunchUtils.Log("开始获取 Minecraft 启动参数");
        // 获取基准字符串与参数信息

        if (!string.IsNullOrEmpty(InstanceManager.Current._versionJson["minecraftArguments"]?.ToString())) {
            McLaunchUtils.Log("获取旧版 Game 参数");
            arguments += " " + McLaunchArgumentsGameOld(InstanceManager.Current);
            McLaunchUtils.Log("旧版 Game 参数获取成功");
        }

        if (InstanceManager.Current._versionJson!.TryGetPropertyValue("arguments", out var argumentNode2) &&
            argumentNode2!.GetValueKind() == JsonValueKind.Object &&
            argumentNode2.AsObject().TryGetPropertyValue("game", out var gameNode)) {
            McLaunchUtils.Log("获取新版 Game 参数");
            arguments += " " + McLaunchArgumentsGameNew(InstanceManager.Current);
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
        var argumentGame = Config.Instance.GameArgs[InstanceManager.Current.Path];
        arguments += " " + (string.IsNullOrEmpty(argumentGame) ? Setup.Get("LaunchAdvanceGame") : argumentGame);

        // 替换参数
        var replaceArguments = McLaunchArgumentsReplace(InstanceManager.Current, loader);
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
            ? Config.Instance.ServerToEnter[InstanceManager.Current.Path]
            : CurrentLaunchOptions.ServerIp;
        if (string.IsNullOrWhiteSpace(worldName) && !string.IsNullOrWhiteSpace(server)) {
            if (InstanceManager.Current.ReleaseTime > new DateTime(2023, 4, 4)) {
                finalArguments += $" --quickPlayMultiplayer \"{server}\"";
            } else {
                if (server.Contains(":")) {
                    var parts = server.Split(':');
                    finalArguments += $" --server {parts[0]} --port {parts[1]}";
                } else {
                    finalArguments += $" --server {server} --port 25565";
                }
                if (InstanceManager.Current.HasPatcher("OptiFine")) {
                    HintWrapper.Show("OptiFine 与自动进入服务器可能不兼容，有概率导致材质丢失甚至游戏崩溃！", HintTheme.Error);
                }
            }
        }

        // 输出
        McLaunchUtils.Log("Minecraft 启动参数：");
        McLaunchUtils.Log(finalArguments);
        McLaunchArgument = finalArguments;
    }
    
    /// <summary>
    /// 为旧版 Minecraft 实例构建 JVM 启动参数。
    /// </summary>
    /// <param name="instance">Minecraft 实例配置。</param>
    /// <returns>以空格分隔的 JVM 参数字符串。</returns>
    /// <exception cref="Exception">当无法连接第三方登录服务器或实例 JSON 缺少 mainClass 时抛出。</exception>
    private string GetMcLaunchArgumentsJvmOld(IMcInstance instance)
    {
        // 初始化参数列表
        List<string> dataList = [
            // 固定 JVM 参数
            "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump"
        ];

        // 获取自定义 JVM 参数
        var argumentJvm = Config.Instance.JvmArgs[instance.Path].IsNullOrEmpty() ? Config.Launch.JvmArgs : Config.Instance.JvmArgs[instance.Path];
        if (!argumentJvm.Contains("-Dlog4j2.formatMsgNoLookups=true")) {
            argumentJvm += " -Dlog4j2.formatMsgNoLookups=true";
        }
        argumentJvm = argumentJvm.Replace(" -XX:MaxDirectMemorySize=256M", ""); // 清理 issue #3511
        dataList.Insert(0, argumentJvm);

        // 设置内存参数
        double ram = InstanceRamService.GetInstanceMemoryAllocation(instance, !McLaunchJavaSelected.Is64Bit) * 1024;
        dataList.Add($"-Xmn{(int)(ram * 0.15)}m");
        dataList.Add($"-Xmx{(int)ram}m");

        // 添加本地库路径和类路径
        dataList.Add($"-Djava.library.path=\"{GetNativesFolder()}\"");
        dataList.Add("-cp ${classpath}");

        // Authlib-Injector 配置
        if (McLoginLoader.Output.Type == "Auth")
        {
            if (McLaunchJavaSelected.JavaMajorVersion >= 6)
            {
                dataList.Add("-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT"); // 信任系统根证书 (Meloong-Git/#5252)
            }

            string server = McLoginAuthLoader.Input.BaseUrl.Replace("/authserver", "");
            try
            {
                string response = NetGetCodeByRequestRetry(server, Encoding.UTF8);
                dataList.Insert(0, $"-javaagent:\"{PathPure}authlib-injector.jar\"={server} " +
                                  $"-Dauthlibinjector.side=client " +
                                  $"-Dauthlibinjector.yggdrasil.prefetched={Convert.ToBase64String(Encoding.UTF8.GetBytes(response))}");
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"无法连接到第三方登录服务器 ({server ?? "null"})\n详细信息: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"无法连接到第三方登录服务器 ({server ?? "null"})", ex);
            }
        }

        // 渲染器配置
        int renderer = Setup.Get("VersionAdvanceRenderer", instance: instance);
        const string MesaLoaderWindowsVersion = "25.1.7";
        string mesaLoaderWindowsTargetFile = $"{PathPure}\\mesa-loader-windows\\{MesaLoaderWindowsVersion}\\Loader.jar";

        if (renderer != 0)
        {
            string rendererType = renderer switch
            {
                1 => "llvmpipe",
                2 => "d3d12",
                _ => "zink"
            };
            dataList.Insert(0, $"-javaagent:\"{mesaLoaderWindowsTargetFile}\"={rendererType}");
        }

        // 代理设置
        if (Setup.Get("VersionAdvanceUseProxyV2", instance: instance) is not null && !string.IsNullOrWhiteSpace(Setup.Get("SystemHttpProxy")))
        {
            Uri proxyAddress = new(Setup.Get("SystemHttpProxy"));
            string scheme = proxyAddress.Scheme.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
            dataList.Add($"-D{scheme}.proxyHost={proxyAddress.Host}");
            dataList.Add($"-D{scheme}.proxyPort={proxyAddress.Port}");
        }

        // Java Wrapper 配置
        if (IsUtf8CodePage() && !Setup.Get("LaunchAdvanceDisableJLW") && !Setup.Get("VersionAdvanceDisableJLW", instance))
        {
            if (McLaunchJavaSelected.JavaMajorVersion >= 9)
            {
                dataList.Add("--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED");
            }
            dataList.Add($"-Doolloo.jlw.tmpdir=\"{PathPure.TrimEnd('\\')}\"");
            dataList.Add($"-jar \"{ExtractJavaWrapper()}\"");
        }

        // 添加主类
        JsonNode? mainClass = instance.JsonObject["mainClass"];
        if (mainClass is null)
        {
            throw new Exception("实例 JSON 中缺少 mainClass 项！");
        }
        dataList.Add(mainClass.ToString());

        return string.Join(" ", dataList);
    }

    /// <summary>
    /// 为新版 Minecraft 实例构建 JVM 启动参数。
    /// </summary>
    /// <param name="instance">Minecraft 实例配置。</param>
    /// <returns>以空格分隔的 JVM 参数字符串。</returns>
    /// <exception cref="Exception">当无法连接第三方登录服务器或实例 JSON 缺少 mainClass 时抛出。</exception>
    private string GetMcLaunchArgumentsJvmNew(McInstance instance)
    {
        // 初始化参数列表
        List<string> dataList = [];

        // 从 JSON 中提取 JVM 参数
        McInstance currentInstance = instance;
        while (true)
        {
            JsonArray? jvmArgs = currentInstance.JsonObject["arguments"]?["jvm"]?.AsArray();
            if (jvmArgs is not null)
            {
                foreach (JsonNode? subJson in jvmArgs)
                {
                    if (subJson is JsonValue value && value.TryGetValue<string>(out var str))
                    {
                        // 字符串类型参数
                        dataList.Add(str);
                    }
                    else if (subJson?["rules"] is not null && McJsonRuleCheck(subJson["rules"]))
                    {
                        // 满足规则的非字符串参数
                        if (subJson["value"] is JsonValue valueNode && valueNode.TryGetValue<string>(out var valueStr))
                        {
                            dataList.Add(valueStr);
                        }
                        else if (subJson["value"] is JsonArray valueArray)
                        {
                            dataList.AddRange(valueArray.Select(v => v?.ToString() ?? ""));
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(currentInstance.InheritInstance))
            {
                break;
            }
            currentInstance = new McInstance(currentInstance.InheritInstance);
        }

        // 添加内存、Log4j 防御等参数
        SecretLaunchJvmArgs(dataList);

        // Authlib-Injector 配置
        if (McLoginLoader.Output.Type == "Auth")
        {
            if (McLaunchJavaSelected.JavaMajorVersion >= 6)
            {
                dataList.Add("-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT"); // 信任系统根证书 (Meloong-Git/#5252)
            }

            string server = McLoginAuthLoader.Input.BaseUrl.Replace("/authserver", "");
            try
            {
                string response = NetGetCodeByRequestRetry(server, Encoding.UTF8);
                dataList.Insert(0, $"-javaagent:\"{PathPure}authlib-injector.jar\"={server} " +
                                  $"-Dauthlibinjector.side=client " +
                                  $"-Dauthlibinjector.yggdrasil.prefetched={Convert.ToBase64String(Encoding.UTF8.GetBytes(response))}");
            }
            catch (Exception ex)
            {
                throw new Exception($"无法连接到第三方登录服务器 ({server ?? "null"})", ex);
            }
        }

        // 渲染器配置
        int renderer = Setup.Get("VersionAdvanceRenderer", instance: instance);
        const string MesaLoaderWindowsVersion = "25.1.7";
        string mesaLoaderWindowsTargetFile = $"{PathPure}\\mesa-loader-windows\\{MesaLoaderWindowsVersion}\\Loader.jar";

        if (renderer != 0)
        {
            string rendererType = renderer switch
            {
                1 => "llvmpipe",
                2 => "d3d12",
                _ => "zink"
            };
            dataList.Insert(0, $"-javaagent:\"{mesaLoaderWindowsTargetFile}\"={rendererType}");
        }

        // 代理设置
        if (Config.Instance.UseProxy.Item(instance.PathIndie) && Config.System.HttpProxy.Type == 2 && !string.IsNullOrWhiteSpace(Config.System.HttpProxy.CustomAddress))
        {
            try
            {
                Uri proxyAddress = new(Setup.Get("SystemHttpProxy"));
                string scheme = proxyAddress.Scheme.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
                dataList.Add($"-D{scheme}.proxyHost={proxyAddress.Host}");
                dataList.Add($"-D{scheme}.proxyPort={proxyAddress.Port}");
            }
            catch (Exception ex)
            {
                Log(ex, "无法将代理信息添加到游戏，放弃加入", LogLevel.Hint);
            }
        }

        // RetroWrapper 参数
        if (McLaunchNeedsRetroWrapper(instance))
        {
            dataList.Add("-Dretrowrapper.doUpdateCheck=false");
        }

        // Java Wrapper 配置
        if (IsUtf8CodePage() && !Setup.Get("LaunchAdvanceDisableJLW") && !Setup.Get("VersionAdvanceDisableJLW", instance))
        {
            if (McLaunchJavaSelected.JavaMajorVersion >= 9)
            {
                dataList.Add("--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED");
            }
            dataList.Add($"-Doolloo.jlw.tmpdir=\"{PathPure.TrimEnd('\\')}\"");
            dataList.Add($"-jar \"{ExtractJavaWrapper()}\"");
        }

        // 去重并合并参数
        List<string> deDuplicateDataList = [];
        for (int i = 0; i < dataList.Count; i++)
        {
            string currentEntry = dataList[i];
            if (currentEntry.StartsWith("-"))
            {
                while (i < dataList.Count - 1 && !dataList[i + 1].StartsWith("-"))
                {
                    currentEntry += " " + dataList[++i];
                }
            }
            deDuplicateDataList.Add(currentEntry.Trim().Replace("McEmu= ", "McEmu="));
        }

        // 清理 issue #3511
        deDuplicateDataList.Remove("-XX:MaxDirectMemorySize=256M");

        // 去重并生成结果
        string result = string.Join(" ", deDuplicateDataList.Distinct());

        // 添加主类
        JsonNode? mainClass = instance.JsonObject["mainClass"];
        if (mainClass is null)
        {
            throw new Exception("实例 JSON 中缺少 mainClass 项！");
        }
        result += $" {mainClass}";

        return result;
    }
}
