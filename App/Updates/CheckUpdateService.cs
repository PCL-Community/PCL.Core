using PCL.Core.App.Updates.Models;
using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;
using System;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
[LifecycleScope("check-update", "检查更新")]
public partial class CheckUpdateService
{
    private static readonly SourceController _SourceController = new([
        new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/", "Pysio")
    ]);
    
    public static VersionData? AvailableVersion { get; set; }
    
    public static bool IsUpdateDownloaded { get; set; }

    [LifecycleStart]
    private static async Task _Start()
    {
        if (Config.System.Update.UpdateMode == 3)
        {
            Context.Info("已设置为不自动检查更新，跳过检查更新步骤");
            return;
        }

        var result = await _TryCheckUpdate();
        if (result == null) return;

        if (!result.IsAvailable)
        {
            Context.Info("当前已是最新版本");
            return;
        }
        
        AvailableVersion = result;
        Context.Info($"发现新版本：{AvailableVersion.VersionName}，准备更新");

        if (Config.System.Update.UpdateMode == 2 && !_PromptUpdate()) return;

        if (!await _TryDownloadUpdate()) return;

        if (Config.System.Update.UpdateMode == 1 && !_PromptInstall()) return;

        Context.Info("准备重启并安装更新包...");
        UpdateHelper.InstallAndRestart(true, true);
    }

    private static async Task<VersionData?> _TryCheckUpdate()
    {
        try
        {
            return await _SourceController.CheckUpdateAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            Context.Warn("所有更新源均不可用", ex);
            HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
            return null;
        }
        catch (Exception ex)
        {
            Context.Warn("检查更新时发生未知异常", ex);
            HintWrapper.Show("检查更新时发生未知异常，可能是网络问题", HintTheme.Error);
            return null;
        }
    }

    private static async Task<bool> _TryDownloadUpdate()
    {
        Context.Info("正在下载更新包...");
        try
        {
            await _SourceController.DownloadAsync("").ConfigureAwait(false);
            Context.Info("更新包下载完成");
            IsUpdateDownloaded = true;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Context.Warn("所有更新源均不可用", ex);
            HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
            return false;
        }
        catch (Exception ex)
        {
            Context.Warn("下载更新包时发生未知异常", ex);
            HintWrapper.Show("下载更新包时发生未知异常，可能是网络问题", HintTheme.Error);
            return false;
        }
    }

    private static bool _PromptUpdate()
    {
        if (AvailableVersion == null) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {AvailableVersion.VersionName}){Constants.vbCrLf}" +
                $"是否立即下载并安装？{Constants.vbCrLf}" +
                "你也可以稍后在 设置 -> 检查更新 界面中更新。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("用户取消了更新");
        return false;
    }

    private static bool _PromptInstall()
    {
        if (AvailableVersion == null) return false;
        if (!IsUpdateDownloaded) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {AvailableVersion.VersionName}){Constants.vbCrLf}" +
                $"已自动下载，是否立即安装？{Constants.vbCrLf}" +
                "你也可以稍后在 设置 -> 检查更新 界面中安装。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("用户取消了更新");
        return false;
    }
}