using System;
using PCL.Core.App;

namespace PCL.Core.Net.Download;

[LifecycleService(LifecycleState.Loaded)]
public class DownloadService : GeneralService
{
    private static LifecycleContext? _context;

    private static LifecycleContext Context => _context!;

    /// <summary>
    /// 下载任务调度器
    /// </summary>
    private static DownloadScheduler? _scheduler;
    
    public DownloadService() : base("download", "下载管理服务")
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        base.Start();
        _scheduler = new DownloadScheduler();
        Context.Info("下载器已初始化");
        _scheduler.Start();
        Context.Info("下载器调度器已启动, 可以添加下载任务了");
    }

    public static bool AddItem(DownloadItem item)
    {
        if (_scheduler == null)
        {
            Context.Warn("下载器未初始化，无法添加下载项");
            return false;
        }
        
        _scheduler.AddItem(item);
        Context.Info($"{item.SourceUri} 已添加入下载队列");
        return true;
    }
    
    public override void Stop()
    {
        base.Stop();
        if (_scheduler == null) return;
        
        _scheduler.Cancel();
        _scheduler = null;
        Context.Info("下载器已释放");
    }
}