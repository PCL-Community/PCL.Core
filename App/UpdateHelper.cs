using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.App;

public static class UpdateHelper
{
    /// <summary>
    /// 更新启动器（替换文件）
    /// </summary>
    /// <param name="source">用于替换的来源文件路径</param>
    /// <param name="target">目标文件路径</param>
    public static Exception? Replace(string source, string target)
    {
        var backup = $"{target}.bak.{DateTime.Now:yyyyMMddHHmmss}";
        Exception? lastEx = null;
        try
        {
            source = Path.GetFullPath(source);
            target = Path.GetFullPath(target);
            // 备份目标文件
            File.Copy(target, backup);
            if (!File.Exists(backup)) throw new FileNotFoundException("备份目标文件失败", backup);
            // 删除原文件并等待文件删除事件
            var watcher = new FileSystemWatcher(Basics.GetParentPathOrEmpty(target), Path.GetFileName(target));
            var deletedEvent = new ManualResetEventSlim(false);
            watcher.Deleted += (_, _) => deletedEvent.Set();
            watcher.EnableRaisingEvents = true;
            File.Delete(target);
            if (!deletedEvent.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException("删除目标文件失败");
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            // 复制到目标文件
            File.Copy(source, target);
            if (!File.Exists(target)) throw new FileNotFoundException("复制到目标文件失败", target);
        }
        catch (Exception ex)
        {
            // 出错：恢复原文件并返回异常
            if (File.Exists(backup) && !File.Exists(target)) File.Move(backup, target);
            lastEx = ex;
        }
        if (File.Exists(backup)) File.Delete(backup); // 删除备份文件
        return lastEx;
    }
    
    [ArgumentHandler("Update")]
    public static HandleResult HandleArgument(string[] args)
    {
        switch (args)
        {
            case ["update_finished", var toDelete]:
            {
                File.Delete(toDelete);
                LogWrapper.Debug("Argument", "更新来源文件已删除");
                return new HandleResult(HandleResultType.Handled);
            }
            case ["update_failed", var reason]:
            {
                LogWrapper.Error("Argument",
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
                    LogWrapper.Info("Argument", "开始更新");
                    Lifecycle.PendingLogDirectory = Path.Combine(Basics.ExecutableDirectory, "Log");
                    Lifecycle.PendingLogFileName = "LastPending_Update.log";

                    var oldPid = strOldPid.Convert<int>();
                    LogWrapper.Debug("Argument", $"旧版本进程 PID: {oldPid}");
                    try
                    {
                        var oldProcess = Process.GetProcessById(oldPid);
                        LogWrapper.Debug("Argument", "等待旧版本进程退出");
                        oldProcess.WaitForExit();
                        LogWrapper.Trace("Argument", "旧版本进程已退出");
                    }
                    catch
                    {
                        /* ignored */
                    }

                    LogWrapper.Debug("Argument", "正在替换文件");
                    LogWrapper.Trace("Argument", $"目标: {target}");
                    LogWrapper.Trace("Argument", $"来源: {source}");
                    var ex = Replace(source, target);
                    if (ex == null) LogWrapper.Trace("Argument", "替换完成");
                    else LogWrapper.Error(ex, "Argument", "替换文件出错");

                    var restart = strRestart.Convert<bool>();
                    if (restart)
                    {
                        var restartArgs = (ex == null) ? $"finished \"{source}\"" : $"failed \"{ex.Message}\"";
                        restartArgs = $"update_{restartArgs}";
                        LogWrapper.Debug("Argument", $"重启中，使用参数: {restartArgs}");
                        Process.Start(target, restartArgs);
                    }
                }
                catch (Exception ex)
                {
                    LogWrapper.Error(ex, "Argument", "更新过程出错");
                }

                return new HandleResult(HandleResultType.HandledAndExit);
            }
            default:
            {
                LogWrapper.Debug("Argument", "非更新参数");
                return new HandleResult(HandleResultType.NotHandled);
            }
        }
    }
}
