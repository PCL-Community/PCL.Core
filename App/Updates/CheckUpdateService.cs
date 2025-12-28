using PCL.Core.App.Updates.Models;
using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
[LifecycleScope("check_update", "检查更新")]
public partial class CheckUpdateService
{
    private static readonly SourceController _SourceController = new([
        new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/", "Pysio")
    ]);

    [LifecycleStart]
    private async Task _Start()
    {
        CheckResult result;
        try
        {
            result = await _SourceController.CheckUpdateAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is TaskCanceledException)
        {
            Context.Warn("检查更新时发生异常", ex);
            HintWrapper.Show("检查更新时发生异常，可能是网络问题导致", HintTheme.Error);
            return;
        }
        catch (Exception ex)
        {
            Context.Warn("检查更新时发生未知异常", ex);
            throw;
        }

        switch (result.Type)
        {
            case CheckResultType.Available: break;
            case CheckResultType.Latest:
                {
                    Context.Info("当前已是最新版本");
                    return;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Type), "Update checkout result out of range");
        }

        if (result.VersionData == null)
        {
            Context.Warn("检查更新失败，版本信息为 null");
            HintWrapper.Show("检查更新失败，可能是网络问题导致", HintTheme.Error);
            return;
        }

        Context.Info("发现新版本, 准备下载更新包...");

        var answer = MsgBoxWrapper.Show(result.VersionData.ChangeLog,
            "发现新版本",
            MsgBoxTheme.Info,
            true,
            "更新",
            "取消");

        if (answer != 1)
        {
            Context.Info("用户取消更新");
            return;
        }

        Context.Info("正在下载更新包...");
        try
        {
            await _SourceController.DownloadAsync("").ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is TaskCanceledException)
        catch (Exception ex)
        {
            Context.Warn("下载更新包时发生未知异常", ex);
            throw;
        }
        {
            Context.Warn("下载更新包时发生异常", ex);
            HintWrapper.Show("下载更新包时发生异常，可能是网络问题导致", HintTheme.Error);
            return;
        }
        Context.Info("更新包下载完成，准备启动更新程序...");

        // UpdateHelper.Restart(true);
        // 因为 UpdateMinioSource.DownloadAsync 还没实现，所以先不启动更新程序
    }
}