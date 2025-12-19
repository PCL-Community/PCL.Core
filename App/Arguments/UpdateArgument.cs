using System;
using System.Diagnostics;
using System.IO;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App.Arguments;

public static class UpdateArgument
{
    private static LifecycleContext Context => ArgumentService.Context;
    
    [ArgumentHandler("Update")]
    public static HandleResult HandleArgument(string[] args)
    {
        switch (args)
        {
            case ["update_finished", var toDelete]:
            {
                File.Delete(toDelete);
                Context.Debug("更新来源文件已删除");
                return new HandleResult(HandleResultType.Handled);
            }
            case ["update_failed", var reason]:
            {
                Context.Error(
                    $"更新失败: {reason}\n你可以手动将 exe 文件替换为 PCL 目录中的新版本" +
                    $"或再次尝试更新，若再次尝试仍然失败，请尽快反馈这个问题");
                return new HandleResult(HandleResultType.Handled);
            }
            case ["update", 
                var strOldPid,
                var target, 
                var source, 
                var strRestart]:
            {
                try
                {
                    Context.Info("开始更新");
                    Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log");
                    Lifecycle.PendingLogFileName = "LastPending_Update.log";

                    var oldPid = strOldPid.Convert<int>();
                    Context.Debug($"旧版本进程 PID: {oldPid}");
                    try
                    {
                        var oldProcess = Process.GetProcessById(oldPid);
                        Context.Debug("等待旧版本进程退出");
                        oldProcess.WaitForExit();
                        Context.Trace("旧版本进程已退出");
                    }
                    catch
                    {
                        /* ignored */
                    }

                    Context.Debug("正在替换文件");
                    Context.Trace($"目标: {target}");
                    Context.Trace($"来源: {source}");
                    var ex = UpdateHelper.Replace(source, target);
                    if (ex == null) Context.Trace("替换完成");
                    else Context.Error("替换文件出错", ex);

                    var restart = strRestart.Convert<bool>();
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

                return new HandleResult(HandleResultType.HandledAndExit);
            }
            default:
            {
                Context.Debug("非更新参数");
                return new HandleResult(HandleResultType.NotHandled);
            }
        }
    }
}