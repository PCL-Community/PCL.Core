using System;
using System.Diagnostics;
using System.IO;
using PCL.Core.App.Arguments;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App;

[ArgumentHandler]
public sealed class UpdateHandler() : GeneralHandler("Update")
{
    public override HandleResult Handle(string[] args)
    {
        switch (args)
        {
            case ["update_finished", var toDelete]:
            {
                File.Delete(toDelete);
                ParentContext.Debug("更新来源文件已删除");
                return HandleResult.Handled;
            }
            case ["update_failed", var reason]:
            {
                ParentContext.Error(
                    $"更新失败: {reason}\n你可以手动将 exe 文件替换为 PCL 目录中的新版本" +
                    $"或再次尝试更新，若再次尝试仍然失败，请尽快反馈这个问题");
                return HandleResult.Handled;
            }
            case ["update", 
                var strOldPid,
                var target, 
                var source, 
                var strRestart]:
            {
                try
                {
                    ParentContext.Info("开始更新");
                    Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log");
                    Lifecycle.PendingLogFileName = "LastPending_Update.log";

                    var oldPid = strOldPid.Convert<int>();
                    ParentContext.Debug($"旧版本进程 ID: {oldPid}");
                    try
                    {
                        var oldProcess = Process.GetProcessById(oldPid);
                        ParentContext.Debug("正在等待旧版本进程退出");
                        oldProcess.WaitForExit();
                        ParentContext.Trace("旧版本进程已退出");
                    }
                    catch
                    {
                        /* ignored */
                    }

                    ParentContext.Debug("正在替换文件");
                    ParentContext.Trace($"目标: {target}");
                    ParentContext.Trace($"来源: {source}");
                    var ex = UpdateHelper.Replace(source, target);
                    if (ex == null) ParentContext.Trace("替换完成");
                    else ParentContext.Error("替换文件出错", ex);

                    var restart = strRestart.Convert<bool>();
                    if (restart)
                    {
                        var restartArgs = (ex == null) ? $"finished \"{source}\"" : $"failed \"{ex.Message}\"";
                        restartArgs = $"update_{restartArgs}";
                        ParentContext.Debug($"重启中，使用参数: {restartArgs}");
                        Process.Start(target, restartArgs);
                    }
                }
                catch (Exception ex)
                {
                    ParentContext.Error("更新过程出错", ex);
                }

                return HandleResult.HandledAndExit;
            }
            default:
            {
                ParentContext.Debug("非更新参数");
                return HandleResult.NotHandled;
            }
        }
    }
}
