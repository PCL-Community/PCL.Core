using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Sources;
using PCL.Core.UI;
using PCL.Core.Utils.Exts;

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
        CheckUpdate().ConfigureAwait(false).GetAwaiter().GetResult();
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

[LifecycleService(LifecycleState.BeforeLoading)]
public sealed class UpdateArgumentsService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    private UpdateArgumentsService() : base("update", "更新参数检查", false) { _context = ServiceContext; }

    public override void Start()
    {
        var args = Basics.CommandLineArguments;
        
        if (args is not ["update", _, _, _, _])
        {
            switch (args)
            {
                case ["update_finished", _]:
                {
                    var toDelete = args[1];
                    File.Delete(toDelete);
                    Context.Debug("更新来源文件已删除");
                    break;
                }
                case ["update_failed", _]:
                {
                    var reason = args[1];
                    Context.Error(
                        $"更新失败: {reason}\n你可以手动将 exe 文件替换为 PCL 目录中的新版本" +
                        $"或再次尝试更新，若再次尝试仍然失败，请尽快反馈这个问题");
                    break;
                }
                default: Context.Debug("无更新任务"); break;
            }
            Context.DeclareStopped();
            return;
        }

        try
        {
            Context.Info("开始更新");
            Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log");
            Lifecycle.PendingLogFileName = "LastPending_Update.log";

            var oldProcessId = args[1].Convert<int>();
            Context.Debug($"旧版本进程 ID: {oldProcessId}");
            try
            {
                var oldProcess = Process.GetProcessById(oldProcessId);
                Context.Debug("正在等待旧版本进程退出");
                oldProcess.WaitForExit();
                Context.Trace("旧版本进程已退出");
            }
            catch
            {
                /* ignored */
            }

            Context.Debug("正在替换文件");
            var target = args[2];
            Context.Trace($"目标: {target}");
            var source = args[3];
            Context.Trace($"来源: {source}");
            var ex = UpdateHelper.Replace(source, target);
            if (ex == null) Context.Trace("替换完成");
            else Context.Error("替换文件出错", ex);

            var restart = args[4].Convert<bool>();
            if (restart)
            {
                var restartArgs = (ex == null) ? $"finished \"{source}\"" : $"failed \"{ex.Message}\"";
                restartArgs = $"update_{restartArgs}";
                Context.Debug($"重启中，使用参数: {restartArgs}");
                Process.Start(target, restartArgs);
            }
        }
        catch (Exception ex)
        {
            Context.Error("更新过程出错", ex);
        }
        
        Context.RequestExit();
    }
}
