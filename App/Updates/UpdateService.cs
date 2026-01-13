using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
[LifecycleScope("update", "检查更新")]
public sealed partial class UpdateService
{
    private static readonly SourceController _SourceController = new([
        new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/", "Pysio"),
        new UpdateMinioSource("https://staticassets.naids.com/resources/pclce/", "Naids")
    ]);
    
    public static VersionData? LatestVersion { get; private set; }
    
    public static bool IsUpdateDownloaded { get; set; }

    [LifecycleStart]
    private static async Task _Start()
    {
        if (Config.System.Update.UpdateMode == 3)
        {
            Context.Info("Update mode set to manual, skipping auto-check");
            return;
        }

        Context.Info("Starting update check...");
        if (!await TryCheckUpdate() || LatestVersion is null) return;
        
        if (!LatestVersion.IsAvailable)
        {
            Context.Info("Already on the latest version");
            return;
        }
        
        Context.Info($"New version found: {LatestVersion.VersionCode}, preparing update");

        if (Config.System.Update.UpdateMode == 2 && !_PromptUpdate()) return;

        if (!await TryDownloadUpdate()) return;

        if (Config.System.Update.UpdateMode == 1 && !_PromptInstall()) return;

        Context.Info("Preparing to restart and install update...");
        UpdateHelper.Restart(true, true);
    }

    public static async Task<bool> TryCheckUpdate()
    {
        try
        {
            LatestVersion = await _SourceController.CheckUpdateAsync().ConfigureAwait(false);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Context.Warn("All update sources are unavailable", ex);
            HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
        }
        catch (Exception ex)
        {
            Context.Warn("Unknown exception occurred while checking updates", ex);
            HintWrapper.Show("检查更新时发生未知异常，可能是网络问题", HintTheme.Error);
        }
        return false;
    }

    public static async Task<bool> TryDownloadUpdate()
    {
        Context.Info("Downloading update package...");
        try
        {
            var outputPath = Path.Combine(
                Basics.ExecutablePath, 
                "PCL", 
                "Plain Craft Launcher Community Edition.exe");
            if (LatestVersion == null) return false;
            await _SourceController.DownloadAsync(outputPath).ConfigureAwait(false);
            Context.Info("Update package downloaded successfully");
            IsUpdateDownloaded = true;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Context.Warn("All update sources are unavailable", ex);
            HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
            return false;
        }
        catch (Exception ex)
        {
            Context.Warn("Unknown exception occurred while checking updates", ex);
            HintWrapper.Show("下载更新包时发生未知异常，可能是网络问题", HintTheme.Error);
            return false;
        }
    }

    private static bool _PromptUpdate()
    {
        if (LatestVersion == null) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {LatestVersion.VersionName}){Constants.vbCrLf}" +
                $"是否立即下载并安装？{Constants.vbCrLf}" +
                "你也可以稍后在 设置 -> 检查更新 界面中更新。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("User cancelled update");
        return false;
    }

    private static bool _PromptInstall()
    {
        if (LatestVersion == null) return false;
        if (!IsUpdateDownloaded) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {LatestVersion.VersionName}){Constants.vbCrLf}" +
                $"已自动下载，是否立即安装？{Constants.vbCrLf}" +
                "你也可以稍后在 设置 -> 检查更新 界面中安装。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("User cancelled update");
        return false;
    }
}