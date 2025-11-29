using System;
using PCL.Core.App.Updates.Sources;
using PCL.Core.Logging;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.Running)]
public class CheckUpdateService : GeneralService
{
    private static LifecycleContext? _context;
    
    private static LifecycleContext Context => _context!;

    public CheckUpdateService() : base("check_update", "检查更新") { _context = ServiceContext; }
    
    public override void Start()
    {
        var source = new UpdateMinioSource("https://s3.pysio.online/pcl2-ce/", "Pysio");
        var result = source.CheckUpdateAsync().GetAwaiter().GetResult();
        
        switch (result.Type)
        {
            case CheckUpdateResultType.HasNewVersion:
            {
                Context.Info("发现新版本, 准备下载更新包...");
                
                // TODO: 提示用户有新版本可用 (等待 MsgBoxWrapper 实现提示功能)
                
                Context.Info("正在下载更新包...");
                source.DownloadAsync("").GetAwaiter().GetResult();
                Context.Info("更新包下载完成，准备启动更新程序...");
                
                // TODO: 启动更新程序
                break;
            }
            case CheckUpdateResultType.NoNewVersion:
            {
                Context.Info("当前已是最新版本");
                
                // TODO: 提示用户无需更新 (等待 HintWrapper 实现提示功能)
                break;
            }
            case CheckUpdateResultType.CheckFailed:
            {
                Context.Warn("检查更新失败");
                
                // TODO: 提示用户检查更新失败 (等待 HintWrapper 实现提示功能)
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        Context.DeclareStopped();
    }
}