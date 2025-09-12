using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Minecraft.Instance;
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
        var (minVer, maxVer) = await InstanceJavaService.GetCompatibleJavaVersionRange(McInstanceManager.Current);
        var javaManager = JavaService.JavaManager;
        var javaInfos = await javaManager.SelectSuitableJava(minVer, maxVer);
        var javaInfo = javaInfos.FirstOrDefault();
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
            if (McInstanceManager.Current.GetInstanceInfo()!.HasPatcher("forge")) {
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
}
