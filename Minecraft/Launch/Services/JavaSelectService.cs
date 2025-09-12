using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Instance;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Service;
using PCL.Core.Minecraft.Launch.State;
using PCL.Core.UI;

namespace PCL.Core.Minecraft.Launch.Services;

/// <summary>
/// Java版本选择器
/// </summary>
public static class JavaSelectService {
    /// <summary>
    /// 根据版本要求选择最佳Java
    /// </summary>
    public static async Task<JavaInfo> SelectBestJavaAsync() {
        var (minVer, maxVer) = InstanceJavaService.GetCompatibleJavaVersionRange(InstanceManager.Current!,
            InstanceManager.CurrentJsonBased.VersionJson!, InstanceManager.CurrentJsonBased.VersionJsonInJar);
        var javaManager = JavaService.JavaManager;
        var javaInfos = JavaSelect(minVer, maxVer, instance);
        if (javaInfo != null) {
            McLaunchUtils.Log($"选择的 Java：{javaInfo}");
            return javaInfo;
        }

        McLaunchUtils.Log("无合适的 Java，需要确认是否自动下载");
        string javaCode;
        var needJavaAutoDownload = true;
        if (minVer >= new Version(22, 0)) // 潜在的向后兼容
        {
            javaCode = minVer.Minor.ToString();
            if (!JavaDownloadConfirm($"Java {javaCode}")) {
                needJavaAutoDownload = false;
            }
        } else if (minVer >= new Version(21, 0)) {
            javaCode = "21";
            if (!JavaDownloadConfirm("Java 21")) {
                needJavaAutoDownload = false;
            }
        } else if (minVer >= new Version(1, 9)) {
            javaCode = "17";
            if (!JavaDownloadConfirm("Java 17")) {
                needJavaAutoDownload = false;
            }
        } else if (maxVer < new Version(1, 8)) {
            javaCode = "7";
            if (InstanceManager.Current!.InstanceInfo.HasPatcher("forge")) {
                MsgBoxWrapper.Show("你需要先安装 LegacyJavaFixer Mod，或自行安装 Java 7，然后才能启动该版本。", "未找到 Java");
            } else {
                if (!JavaDownloadConfirm("Java 7", true)) {
                    needJavaAutoDownload = false;
                }
            }
        } else if (minVer > new Version(1, 8, 0, 140) && maxVer < new Version(1, 8, 0, 321)) {
            javaCode = "8u141";
            if (!JavaDownloadConfirm("Java 8.0.141 ~ 8.0.320", true)) {
                needJavaAutoDownload = false;
            }
        } else if (minVer > new Version(1, 8, 0, 140)) {
            javaCode = "8u141";
            if (!JavaDownloadConfirm("Java 8.0.141 或更高版本的 Java 8", true)) {
                needJavaAutoDownload = false;
            }
        } else if (maxVer < new Version(1, 8, 0, 321)) {
            javaCode = "8";
            if (!JavaDownloadConfirm("Java 8.0.320 或更低版本的 Java 8")) {
                needJavaAutoDownload = false;
            }
        } else {
            javaCode = "8";
            if (!JavaDownloadConfirm("Java 8")) {
                needJavaAutoDownload = false;
            }
        }

        if (needJavaAutoDownload) {
            // TODO: JAVA下载逻辑
            /*
            // 开始自动下载
            var JavaLoader = JavaFixLoaders(javaCode);
            try {
                JavaLoader.Start(javaCode, IsForceRestart: true);
                while (JavaLoader.State == LoadState.Loading && !Task.IsAborted) {
                    Task.Progress = JavaLoader.Progress;
                    Thread.Sleep(10);
                }
            } finally {
                JavaLoader.Abort(); // 确保取消时中止 Java 下载
            }

            // 检查下载结果
            McLaunchJavaSelected = JavaSelect("$$", minVer, maxVer, McInstanceCurrent);
            if (Task.IsAborted) return;
            if (McLaunchJavaSelected != null) {
                McLaunchLog($"选择的 Java：{McLaunchJavaSelected.ToString()}");
            } else {
                Hint("没有可用的 Java，已取消启动！", HintType.Critical);
                throw new Exception("$$");
            }
            */
        }
        return javaInfo!;
    }

    private static bool JavaDownloadConfirm(string javaCode, bool forcedManualDownload = false) {
        if (forcedManualDownload) {
            MsgBoxWrapper.Show($"PCL 未找到 {javaCode}。\n" +
                               $"请自行搜索并安装 {javaCode}，安装后在 设置 → 启动选项 → 游戏 Java 中重新搜索或导入。",
                "未找到 Java");
            return false;
        }
        return MsgBoxWrapper.Show($"PCL 未找到 {javaCode}，是否需要 PCL 自动下载？\n" +
                                  $"如果你已经安装了 {javaCode}，可以在 设置 → 启动选项 → 游戏 Java 中手动导入。",
            "自动下载 Java？", buttons: ["自动下载", "取消"]) == 1;
    }

    /// <summary>
    /// 根据要求选择最适合的 Java 版本，若找不到则返回 null。
    /// 最小与最大版本在与输入相同时也会通过。
    /// 必须在工作线程调用，且必须使用 lock (JavaLock) 确保线程安全。
    /// </summary>
    /// <param name="minVer">要求的最低 Java 版本，可为 null 表示无限制。</param>
    /// <param name="maxVer">要求的最大 Java 版本，可为 null 表示无限制。</param>
    /// <param name="instance">关联的 Minecraft 实例，可为 null 表示无关联实例。</param>
    /// <returns>适合的 Java 版本，或 null 如果未找到。</returns>
    /// <remarks>
    /// 按以下优先级选择 Java：
    /// 1. 实例指定的 Java（relatedVersion）。
    /// 2. 用户全局指定的 Java（LaunchArgumentJavaSelect）。
    /// 3. 自动搜索并选择适合的 Java。
    /// </remarks>
    public static async Task<JavaInfo?> JavaSelect(Version minVer, Version maxVer, IMcInstance instance) {
        // 记录选择要求
        LogWrapper.Info($"要求选择合适 Java，最低版本：{minVer.ToString()}，最高版本：{maxVer.ToString()}，关联实例：{instance.Name}");

        // 优先检查实例指定的 Java
        var userVersionJava = GetVersionUserSetJava(instance);
        if (userVersionJava is not null) {
            if (!IsVersionSuit(userVersionJava.Version)) {
                HintWrapper.Show("当前实例所指定的 Java 可能不合适，容易导致游戏崩溃");
            }
            LogWrapper.Info($"返回实例 {instance.Name} 指定的 Java {userVersionJava}");
            return userVersionJava;
        }

        // 检查用户全局指定的 Java
        var userGlobalJava = Config.Launch.SelectedJava;
        var userGlobalJavaSet = JavaInfo.Parse(userGlobalJava);
        if (userGlobalJavaSet is not null) {
            LogWrapper.Info($"返回全局指定的 Java {userGlobalJavaSet}");
            return userGlobalJavaSet;
        }

        // 自动搜索适合的 Java
        JavaService.JavaManager.CheckJavaAvailability();
        var ret = (await JavaService.JavaManager.SelectSuitableJava(minVer, maxVer)).FirstOrDefault();

        if (ret is null) {
            LogWrapper.Info("没有找到合适的 Java，开始尝试重新搜索后选择");
            await JavaService.JavaManager.ScanJavaAsync();
            ret = (await JavaService.JavaManager.SelectSuitableJava(minVer, maxVer)).FirstOrDefault();
        }

        LogWrapper.Info($"返回自动选择的 Java {ret?.ToString() ?? "无结果"}");
        return ret;
        
        bool IsVersionSuit(Version ver) => ver >= minVer && ver <= maxVer;
    }

    /// <summary>
    /// 获取指定 Minecraft 实例所要求的 Java 版本。
    /// </summary>
    /// <returns>如果实例指定了 Java 版本，则返回对应的 Java 对象；否则返回 null。</returns>
    private static JavaInfo? GetVersionUserSetJava(IMcInstance instance) {
        var userSetupVersion = Config.Instance.SelectedJava[instance.Path];
        if (userSetupVersion == "使用全局设置") {
            return null;
        }

        return JavaInfo.Parse(userSetupVersion);
    }
}
