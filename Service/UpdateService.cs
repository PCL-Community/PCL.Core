using System;
using System.Diagnostics;
using System.IO;
using PCL.Core.Extension;
using PCL.Core.Helper;
using PCL.Core.LifecycleManagement;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.BeforeLoading)]
public sealed class UpdateService : GeneralService
{
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;

    private UpdateService() : base("update", "更新", false) { _context = ServiceContext; }

    public override void Start()
    {
        var args = Environment.GetCommandLineArgs();
        
        if (args is not [_, "update", _, _, _, _])
        {
            if (args is [_, "restart", "update_finished" or "update_failed", _])
            {
                if (args[2] == "update_finished")
                {
                    var toDelete = args[3];
                    File.Delete(toDelete);
                    Context.Debug("更新来源文件已删除");
                }
                else
                {
                    var reason = args[3];
                    Context.Error(
                        $"更新失败: {reason}\n你可以手动将 exe 文件替换为 PCL 目录中的新版本" +
                        $"或再次尝试更新，若再次尝试仍然失败，请尽快反馈这个问题");
                }
            }
            else Context.Debug("无更新任务");
            Context.DeclareStopped();
            return;
        }

        try
        {
            Context.Info("开始更新");
            Lifecycle.PendingLogDirectory = Path.Combine(NativeInterop.ExecutableDirectory, "Log");
            Lifecycle.PendingLogFileName = "LastPending_Update.log";

            var oldProcessId = args[2].Convert<int>();
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
            var target = args[3]!;
            var targetDir = Path.GetDirectoryName(target) ?? Path.GetPathRoot(target);
            Context.Trace($"目标: {target}");
            var source = args[4]!;
            Context.Trace($"来源: {source}");
            var ex = UpdateHelper.Replace(source, target);
            if (ex == null) Context.Trace("替换完成");
            else Context.Error("替换文件出错", ex);

            var restart = args[5].Convert<bool>();
            if (restart)
            {
                var restartArgs = (ex == null) ? $"finished \"{source}\"" : $"failed \"{ex.Message}\"";
                restartArgs = $"restart update_{restartArgs}";
                Context.Debug($"重启中，使用参数: {restartArgs}");
                var psi = new ProcessStartInfo(target, restartArgs) { WorkingDirectory = targetDir };
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
