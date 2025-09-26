using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Service;
using PCL.Core.Minecraft.Launch.Utils;
using PCL.Core.Utils.Codecs;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Launch.Services.Argument;

/// <summary>
/// 构建 Minecraft JVM 启动参数的工具类
/// </summary>
public class JvmArgBuilder(IMcInstance instance) {
    private const string MesaLoaderVersion = "25.1.7";
    private const string HeapDumpParameter = "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump";
    private const string Log4jSecurityParameter = "-Dlog4j2.formatMsgNoLookups=true";
    private const string MaxDirectMemoryParameter = "-XX:MaxDirectMemorySize=256M";

    /// <summary>
    /// 为旧版 Minecraft 实例构建 JVM 启动参数
    /// </summary>
    /// <param name="selectedJava">已选择的 Java 信息</param>
    /// <returns>JVM 参数字符串列表</returns>
    /// <exception cref="InvalidOperationException">当实例 JSON 缺少 mainClass 时抛出</exception>
    public List<string> BuildLegacyJvmArguments(JavaInfo selectedJava) {
        var arguments = new List<string> { HeapDumpParameter };

        AddCustomJvmArguments(arguments);
        AddMemoryArguments(arguments, selectedJava);
        AddNativeLibraryPath(arguments);
        AddClassPath(arguments);
        AddRendererConfiguration(arguments);
        AddProxyConfiguration(arguments);
        AddJavaWrapperConfiguration(arguments, selectedJava);
        AddMainClass(arguments);
        
        AddAccountSystemParametersOld(arguments);

        return arguments;
    }

    /// <summary>
    /// 为新版 Minecraft 实例构建 JVM 启动参数
    /// </summary>
    /// <param name="selectedJava">已选择的 Java 信息</param>
    /// <returns>JVM 参数字符串列表</returns>
    /// <exception cref="InvalidOperationException">当实例 JSON 缺少 mainClass 时抛出</exception>
    public List<string> BuildModernJvmArguments(JavaInfo selectedJava) {
        var arguments = new List<string>();

        AddVersionJsonJvmArguments(arguments);
        AddCommonJvmArguments(arguments);
        AddRendererConfiguration(arguments);
        AddProxyConfiguration(arguments);
        AddRetroWrapperConfiguration(arguments);
        AddJavaWrapperConfiguration(arguments, selectedJava);
        
        AddAccountSystemParametersModern(arguments);

        var processedArguments = ProcessAndDeduplicateArguments(arguments);
        
        AddMainClass(processedArguments);
        return processedArguments;
    }

    #region 私有方法 - 参数构建

    /// <summary>
    /// 添加自定义 JVM 参数
    /// </summary>
    private void AddCustomJvmArguments(List<string> arguments) {
        var customArgs = GetCustomJvmArguments();
        arguments.Insert(0, customArgs);
    }

    /// <summary>
    /// 获取自定义 JVM 参数，确保包含 Log4j 安全参数
    /// </summary>
    private string GetCustomJvmArguments() {
        var customArgs = Config.Instance.JvmArgs[instance.Path].IsNullOrEmpty()
            ? Config.Launch.JvmArgs
            : Config.Instance.JvmArgs[instance.Path];

        if (!customArgs.Contains(Log4jSecurityParameter)) {
            customArgs += $" {Log4jSecurityParameter}";
        }

        // 清理已知问题参数 (issue #3511)
        customArgs = customArgs.Replace($" {MaxDirectMemoryParameter}", "");

        return customArgs;
    }

    /// <summary>
    /// 添加内存相关参数
    /// </summary>
    private void AddMemoryArguments(List<string> arguments, JavaInfo selectedJava) {
        var ramInMb = InstanceRamService.GetInstanceMemoryAllocation(instance, !selectedJava.Is64Bit) * 1024;
        var youngGenSize = (int)(ramInMb * 0.15);

        arguments.Add($"-Xmn{youngGenSize}m");
        arguments.Add($"-Xmx{(int)ramInMb}m");
    }

    /// <summary>
    /// 添加本地库路径
    /// </summary>
    private void AddNativeLibraryPath(List<string> arguments) {
        arguments.Add($"-Djava.library.path=\"{GetNativesFolder()}\"");
    }

    /// <summary>
    /// 添加类路径参数
    /// </summary>
    private void AddClassPath(List<string> arguments) {
        arguments.Add("-cp ${classpath}");
    }

    /// <summary>
    /// 添加渲染器配置
    /// </summary>
    private void AddRendererConfiguration(List<string> arguments) {
        var renderer = Config.Instance.Renderer[instance.Path];
        if (renderer == 0) return;

        var rendererType = GetRendererType(renderer);
        var mesaLoaderPath = GetMesaLoaderPath();

        arguments.Insert(0, $"-javaagent:\"{mesaLoaderPath}\"={rendererType}");
    }

    /// <summary>
    /// 获取渲染器类型字符串
    /// </summary>
    private static string GetRendererType(int renderer) => renderer switch {
        1 => "llvmpipe",
        2 => "d3d12",
        _ => "zink"
    };

    /// <summary>
    /// 获取 Mesa Loader 路径
    /// </summary>
    private static string GetMesaLoaderPath() {
        return Path.Combine(FileService.TempPath, "mesa-loader-windows", MesaLoaderVersion, "Loader.jar");
    }

    /// <summary>
    /// 添加代理配置
    /// </summary>
    private void AddProxyConfiguration(List<string> arguments) {
        if (!ShouldUseProxy()) return;

        try {
            var proxyUri = new Uri(Config.System.HttpProxy.CustomAddress);
            var scheme = GetProxyScheme(proxyUri);

            arguments.Add($"-D{scheme}.proxyHost={proxyUri.Host}");
            arguments.Add($"-D{scheme}.proxyPort={proxyUri.Port}");
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "无法将代理信息添加到游戏，放弃加入");
        }
    }

    /// <summary>
    /// 判断是否应该使用代理
    /// </summary>
    private bool ShouldUseProxy() {
        return Config.Instance.UseProxy[instance.Path] &&
               Config.System.HttpProxy.Type == 2 &&
               !string.IsNullOrWhiteSpace(Config.System.HttpProxy.CustomAddress);
    }
    
    /// <summary>
    /// 账户系统参数（旧版）
    /// </summary>
    private void AddAccountSystemParametersOld(List<string> arguments) {
        // TODO: 等待账户系统
        /*
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
        */
    }

    /// <summary>
    /// 账户系统参数（新版）
    /// </summary>
    private void AddAccountSystemParametersModern(List<string> arguments) {
        // TODO: 等待账户系统
        /*
        // Authlib-Injector 配置
        if (McLoginLoader.Output.Type == "Auth") {
            if (McLaunchJavaSelected.JavaMajorVersion >= 6) {
                dataList.Add("-Djavax.net.ssl.trustStoreType=WINDOWS-ROOT"); // 信任系统根证书 (Meloong-Git/#5252)
            }

            string server = McLoginAuthLoader.Input.BaseUrl.Replace("/authserver", "");
            try {
                string response = NetGetCodeByRequestRetry(server, Encoding.UTF8);
                dataList.Insert(0, $"-javaagent:\"{PathPure}authlib-injector.jar\"={server} " +
                                   $"-Dauthlibinjector.side=client " +
                                   $"-Dauthlibinjector.yggdrasil.prefetched={Convert.ToBase64String(Encoding.UTF8.GetBytes(response))}");
            } catch (Exception ex) {
                throw new Exception($"无法连接到第三方登录服务器 ({server ?? "null"})", ex);
            }
        }
        */
    }

    /// <summary>
    /// 获取代理协议类型
    /// </summary>
    private static string GetProxyScheme(Uri proxyUri) {
        return proxyUri.Scheme.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
    }

    /// <summary>
    /// 添加 RetroWrapper 配置
    /// </summary>
    private void AddRetroWrapperConfiguration(List<string> arguments) {
        if (LaunchEnvUtils.NeedRetroWrapper(instance)) {
            arguments.Add("-Dretrowrapper.doUpdateCheck=false");
        }
    }

    /// <summary>
    /// 添加 Java Wrapper 配置
    /// </summary>
    private void AddJavaWrapperConfiguration(List<string> arguments, JavaInfo selectedJava) {
        if (!ShouldUseJavaWrapper()) return;

        if (selectedJava.JavaMajorVersion >= 9) {
            arguments.Add("--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED");
        }

        arguments.Add($"-Doolloo.jlw.tmpdir=\"{FileService.TempPath}\"");
        arguments.Add($"-jar \"{LaunchEnvUtils.ExtractJavaWrapper()}\"");
    }

    /// <summary>
    /// 判断是否应该使用 Java Wrapper
    /// </summary>
    private bool ShouldUseJavaWrapper() {
        return EncodingUtils.IsDefaultEncodingUtf8() &&
               !Config.Launch.DisableJlw &&
               !Config.Instance.DisableJlw[instance.Path];
    }

    /// <summary>
    /// 添加主类参数
    /// </summary>
    private void AddMainClass(List<string> arguments) {
        var mainClass = GetMainClass();
        arguments.Add(mainClass);
    }

    /// <summary>
    /// 获取主类名称
    /// </summary>
    private string GetMainClass() {
        var mainClass = ((IJsonBasedInstance)instance).VersionJson!["mainClass"];
        if (mainClass is null) {
            throw new InvalidOperationException("实例 JSON 中缺少 mainClass 项！");
        }
        return mainClass.ToString();
    }

    #endregion

    #region 私有方法 - 新版特有逻辑

    /// <summary>
    /// 从版本 JSON 添加 JVM 参数
    /// </summary>
    private void AddVersionJsonJvmArguments(List<string> arguments) {
        var jvmArgs = ((IJsonBasedInstance)instance).VersionJson!["arguments"]?["jvm"]?.AsArray();
        if (jvmArgs is null) return;

        foreach (var argNode in jvmArgs) {
            ProcessJvmArgumentNode(arguments, argNode);
        }
    }

    /// <summary>
    /// 处理单个 JVM 参数节点
    /// </summary>
    private static void ProcessJvmArgumentNode(List<string> arguments, JsonNode argNode) {
        switch (argNode) {
            case JsonValue value when value.TryGetValue<string>(out var str):
                arguments.Add(str);
                break;

            case JsonObject obj when obj["rules"] is not null && McLaunchUtils.CheckRules(obj["rules"]?.AsObject()):
                AddRuleBasedArgument(arguments, obj);
                break;
        }
    }

    /// <summary>
    /// 添加基于规则的参数
    /// </summary>
    private static void AddRuleBasedArgument(List<string> arguments, JsonObject argObject) {
        var valueNode = argObject["value"];
        switch (valueNode) {
            case JsonValue value when value.TryGetValue<string>(out var valueStr):
                arguments.Add(valueStr);
                break;

            case JsonArray valueArray:
                arguments.AddRange(valueArray.Select(v => v?.ToString() ?? ""));
                break;
        }
    }

    /// <summary>
    /// 添加通用 JVM 参数
    /// </summary>
    private void AddCommonJvmArguments(List<string> arguments) {
        AddCustomJvmArgumentsForModern(arguments);
        AddIpStackPreference(arguments);
        AddMemoryArgumentsForModern(arguments);
        AddLog4jSecurity(arguments);
    }

    /// <summary>
    /// 为新版添加自定义 JVM 参数
    /// </summary>
    private void AddCustomJvmArgumentsForModern(List<string> arguments) {
        var customArgs = Config.Instance.JvmArgs[instance.Path];
        var argsToAdd = string.IsNullOrEmpty(customArgs) ? Config.Launch.JvmArgs : customArgs;
        arguments.Insert(0, argsToAdd);
    }

    /// <summary>
    /// 添加 IP 栈偏好设置
    /// </summary>
    private static void AddIpStackPreference(List<string> arguments) {
        switch (Config.Launch.PreferredIpStack) {
            case 0:
                arguments.Add("-Djava.net.preferIPv4Stack=true");
                break;
            case 2:
                arguments.Add("-Djava.net.preferIPv6Stack=true");
                break;
        }
    }

    /// <summary>
    /// 为新版添加内存参数
    /// </summary>
    private void AddMemoryArgumentsForModern(List<string> arguments) {
        LogAvailableMemory();

        var ramInGb = InstanceRamService.GetInstanceMemoryAllocation(instance);
        var ramInMb = ramInGb * 1024;
        var youngGenSize = (int)Math.Floor(ramInMb * 0.15);

        arguments.Add($"-Xmn{youngGenSize}m");
        arguments.Add($"-Xmx{(int)Math.Floor(ramInMb)}m");
    }

    /// <summary>
    /// 记录可用内存信息
    /// </summary>
    private static void LogAvailableMemory() {
        var availableMemoryGb = Math.Round(
            KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024.0 / 1024.0 / 1024.0 * 10
            ) / 10;
        McLaunchUtils.Log($"Current available memory: {availableMemoryGb}G");
    }

    /// <summary>
    /// 添加 Log4j 安全参数
    /// </summary>
    private static void AddLog4jSecurity(List<string> arguments) {
        if (!arguments.Any(arg => arg.Contains(Log4jSecurityParameter))) {
            arguments.Add(Log4jSecurityParameter);
        }
    }

    /// <summary>
    /// 处理和去重参数
    /// </summary>
    private static List<string> ProcessAndDeduplicateArguments(List<string> arguments) {
        var processedArguments = new List<string>();

        for (var i = 0; i < arguments.Count; i++) {
            var currentArg = arguments[i];
            if (currentArg.StartsWith('-')) {
                // 合并连续的非选项参数
                while (i < arguments.Count - 1 && !arguments[i + 1].StartsWith("-")) {
                    currentArg += " " + arguments[++i];
                }
            }
            processedArguments.Add(currentArg.Trim().Replace("McEmu= ", "McEmu="));
        }

        // 移除已知问题参数并去重
        processedArguments.Remove(MaxDirectMemoryParameter);
        return processedArguments.Distinct().ToList();
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 获取 Natives 文件夹路径，不以反斜杠结尾
    /// </summary>
    private string GetNativesFolder() {
        var defaultPath = Path.Combine(instance.Path, $"{instance.Name}-natives");

        if (EncodingUtils.IsDefaultEncodingGbk() || defaultPath.IsASCII()) {
            return defaultPath;
        }

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft", "bin", "natives"
            );

        if (appDataPath.IsASCII()) {
            return appDataPath;
        }

        return Path.Combine(SystemPaths.DriveLetter, "ProgramData", "PCL", "natives");
    }

    #endregion
}
