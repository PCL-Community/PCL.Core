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
    private static DownloadScheduler? _downloader;
    
    public DownloadService() : base("download", "下载管理服务")
    {
        _context = ServiceContext;
    }

    public override void Start()
    {
        base.Start();
        _downloader = new DownloadScheduler();
        Context.Info("下载器已初始化");
    }

    public static bool AddItem(DownloadItem item)
    {
        if (_downloader == null)
        {
            Context.Warn("下载器未初始化，无法添加下载项");
            return false;
        }
        
        _downloader.AddItem(item);
        Context.Info($"{item.SourceUri} 已添加入下载队列");
        return true;
    }
    
    public override void Stop()
    {
        base.Stop();
        if (_downloader == null) return;
        
        _downloader.Cancel();
        _downloader = null;
        Context.Info("下载器已释放");
    }
}