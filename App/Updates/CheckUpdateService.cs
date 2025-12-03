using System;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
public class CheckUpdateService : GeneralService
{
    private static LifecycleContext? _context;
    
    private static LifecycleContext Context => _context!;
    
    private static UpdateMinioSource? _source; // 暂时是这个

    public CheckUpdateService() : base("check_update", "检查更新") { _context = ServiceContext; }
    
    public override void Start()
    {
        _source = new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/",
            "Pysio");
        CheckUpdate(true).Wait();
        Context.DeclareStopped();
    }

    /// <summary>
    /// 检查更新
    /// </summary>
    /// <param name="silent">是否静默更新 (即是否显示 Hint)</param>
    /// <exception cref="IndexOutOfRangeException">检查更新返回值超出范围时抛出</exception>
    public static async Task CheckUpdate(bool silent = false)
    {
        if (_source == null)
        {
            Context.Error("更新源未初始化，无法检查更新");
            return;
        }
        var result = await _source.CheckUpdateAsync().ConfigureAwait(false);
        
        switch (result.Type)
        {
            case CheckUpdateResultType.HasNewVersion: break;
            case CheckUpdateResultType.NoNewVersion:
            {
                Context.Info("当前已是最新版本");
                if (!silent)
                {
                    HintWrapper.Show($"当前已是最新版本 {Basics.VersionName}，无需更新啦", HintType.Finish); // 无新版本时根据参数决定是否提示用户
                }
                return;
            }
            case CheckUpdateResultType.CheckFailed:
            {
                Context.Warn("检查更新失败");
                HintWrapper.Show("检查更新失败，可能是网络问题导致", HintType.Critical); // 失败时无论如何都提示用户
                return;
            }
            default:
                throw new IndexOutOfRangeException("检查更新返回值超出范围");
        }
        
        Context.Info("发现新版本, 准备下载更新包...");
                
        // TODO: 提示用户有新版本可用 (等待 MsgBoxWrapper 实现提示功能)
                
        Context.Info("正在下载更新包...");
        if (!await _source.DownloadAsync("").ConfigureAwait(false))
        {
            Context.Warn("更新包下载失败");
            return;
        }
        Context.Info("更新包下载完成，准备启动更新程序...");
                
        // UpdateHelper.Restart(true);
        // 因为 UpdateMinioSource.DownloadAsync 还没实现，所以先不启动更新程序
    }
}