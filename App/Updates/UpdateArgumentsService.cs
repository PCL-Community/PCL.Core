using PCL.Core.Utils.Exts;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PCL.Core.App.Updates;

[LifecycleService(LifecycleState.BeforeLoading)]
[LifecycleScope("update-arg", "处理更新参数")]
public sealed partial class UpdateArgumentsService
{
    [LifecycleStart]
    private static async Task _Start()
    {
        var args = Basics.CommandLineArguments;

        if (args is not ["update", _, _, _, _])
        {
            await _CheckUpdateResultAsync(args).ConfigureAwait(false);
            return;
        }

        try
        {
            await _UpdateWorkflowAsync(args).ConfigureAwait(false);
        }
        catch (OperationCanceledException ocex)
        {
            // 更新过程被取消
            Context.Error("更新过程被取消", ocex);
        }
        catch (Exception ex)
        {
            Context.Error("更新过程出错", ex);
        }
        finally
        {
            Context.RequestExit();
        }
    }

    #region Private Helper

    private static Task _CheckUpdateResultAsync(string[] args)
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
                        $"更新失败: {reason}\n" +
                        $"你可以手动将 exe 文件替换为 PCL 目录中的新版本或再次尝试更新。\n" +
                        $"若再次尝试仍然失败，请尽快反馈这个问题");
                    break;
                }
            default: Context.Debug("无更新任务"); break;
        }

        Context.DeclareStopped();
        return Task.CompletedTask;
    }

    private static async Task _UpdateWorkflowAsync(string[] args)
    {
        Context.Info("开始更新");

        //Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log"); already set
        Lifecycle.PendingLogFileName = "LastPending_Update.log";

        var oldProcessId = args[1].Convert<int>();
        Context.Debug($"旧版本进程 ID: {oldProcessId}");

        try
        {
            var oldProcess = Process.GetProcessById(oldProcessId);
            Context.Debug("正在等待旧版本进程退出");
            await oldProcess.WaitForExitAsync().ConfigureAwait(false);
            Context.Trace("旧版本进程已退出");
        }
        catch
        {
            // ArgumentException: throws if process not found
            /* ignored */
        }

        Context.Debug("正在替换文件");

        var target = args[2];
        var source = args[3];

        Context.Trace($"目标: {target}");
        Context.Trace($"来源: {source}");
        
        Exception? ex = null;
        try
        {
            File.Replace(source, target, null);
        }
        catch (Exception e)
        {
            ex = e;
        }

        Context.Trace("替换完成");

        var restart = args[4].Convert<bool>();
        if (restart)
        {
            var restartArgs = ex == null ? $"finished \"{source}\"" : $"failed \"{ex.Message}\"";
            restartArgs = $"update_{restartArgs}";
            Context.Debug($"重启中，使用参数: {restartArgs}");
            Process.Start(target, restartArgs);
        }
    }

    #endregion
}