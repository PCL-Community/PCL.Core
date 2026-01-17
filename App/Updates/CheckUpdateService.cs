using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
[LifecycleScope("check-update", "检查更新")]
public sealed partial class CheckUpdateService
{
    private static readonly SourceController _SourceController = new([
        new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/", "Pysio"),
        new UpdateMinioSource("https://staticassets.naids.com/resources/pclce/", "Naids")
    ]);
    
    public static VersionData? LatestVersion { get; private set; }
    
    public static bool IsUpdateDownloaded { get; private set; }

    [LifecycleStart]
    private static async Task _Start()
    {
        if (Config.System.Update.UpdateMode == 3)
        {
            Context.Info("更新模式为禁用，跳过检查");
            return;
        }

        Context.Info("检查更新中...");
        if (!await TryCheckUpdateAsync().ConfigureAwait(false) || LatestVersion is null) return;
        
        if (!LatestVersion.IsAvailable)
        {
            Context.Info("已经是最新版本，跳过更新");
            return;
        }
        
        Context.Info($"发现新版本: {LatestVersion.Code}, 准备更新");

        if (Config.System.Update.UpdateMode == 2 && !_PromptUpdate()) return;

        if (!await TryDownloadUpdateAsync().ConfigureAwait(false)) return;

        if (Config.System.Update.UpdateMode == 1 && !_PromptInstall()) return;

        Context.Info("准备重启并安装...");
        // 这个 UpdateHelper.Restart 使用 Lifecycle.Shutdown 会和动画系统冲突产生各种妙妙小问题，先注释掉
        //UpdateHelper.Restart(true, true);
    }

    #region Public Methods
    
    public static async Task<bool> TryCheckUpdateAsync()
    {
        try
        {
            LatestVersion = await _SourceController.CheckUpdateAsync().ConfigureAwait(false);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("不可用"))
            {
                Context.Warn("所有更新源均不可用", ex);
                HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
            }
            else
            {
                Context.Warn("检查更新时发生未知异常", ex);
                HintWrapper.Show("检查更新时发生未知异常，可能是网络问题", HintTheme.Error);
            }
        }
        catch (Exception ex)
        {
            Context.Warn("检查更新时发生未知异常", ex);
            HintWrapper.Show("检查更新时发生未知异常，可能是网络问题", HintTheme.Error);
        }
        return false;
    }

    public static async Task<bool> TryDownloadUpdateAsync()
    {
        Context.Info("下载更新包中...");
        try
        {
            var outputPath = Path.Combine(
                Basics.ExecutableDirectory, 
                "PCL", 
                "Plain Craft Launcher Community Edition.exe");
            if (LatestVersion == null) return false;
            await _SourceController.DownloadAsync(outputPath).ConfigureAwait(false);
            Context.Info("更新包下载完成");
            IsUpdateDownloaded = true;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("不可用"))
            {
                Context.Warn("所有更新源均不可用", ex);
                HintWrapper.Show("所有更新源均不可用，可能是网络问题", HintTheme.Error);
            }
            else
            {
                Context.Warn("下载更新包时发生未知异常", ex);
                HintWrapper.Show("下载更新包时发生未知异常，可能是网络问题", HintTheme.Error);
            }
        }
        catch (Exception ex)
        {
            Context.Warn("下载更新包时发生未知异常", ex);
            HintWrapper.Show("下载更新包时发生未知异常，可能是网络问题", HintTheme.Error);
        }
        return false;
    }
    
    #endregion

    #region Prompt Wrappers
    
    private static bool _PromptUpdate()
    {
        if (LatestVersion == null) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {LatestVersion.Name})\r\n" +
                $"是否立即下载并安装？\r\n" +
                "你也可以稍后在 设置 -> 检查更新 界面中更新。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("用户取消更新");
        return false;
    }

    private static bool _PromptInstall()
    {
        if (LatestVersion == null) return false;
        if (!IsUpdateDownloaded) return false;

        if (MsgBoxWrapper.Show(
                $"启动器有新版本可用 ({Basics.VersionName} -> {LatestVersion.Name})\r\n" +
                $"已自动下载，是否立即安装？\r\n" +
                "你也可以稍后在 设置 -> 检查更新 界面中安装。",
                "发现新版本", MsgBoxTheme.Info, true, "立刻更新", "以后再说") == 1) return true;
        
        Context.Info("用户取消安装");
        return false;
    }
    
    #endregion
}