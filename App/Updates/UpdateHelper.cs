using PCL.Core.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PCL.Core.App.Updates;

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
    
    /// <summary>
    /// 启动更新程序并重启当前程序。
    /// </summary>
    /// <param name="triggerRestartAndByEnd">是否在启动更新程序后结束当前程序。</param>
    /// <param name="isUpdateRestart">是否为更新重启。</param>
    public static void Restart(bool triggerRestartAndByEnd, bool isUpdateRestart = false)
    {
        try
        {
            var fileName = Path.GetFileName(Environment.ProcessPath);

            if (!File.Exists(fileName))
            {
                LogWrapper.Warn("Update", "更新启动器文件不存在，无法启动更新程序");
                return;
            }


            var startInfo = new ProcessStartInfo(fileName)
            {
                ArgumentList =
                {
                    "update",
                    Environment.ProcessId.ToString(),
                    $"{Basics.ExecutablePath}",
                    $"{fileName}",
                    isUpdateRestart ? "true" : "false"
                },
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            LogWrapper.Info("Update", "已尝试启动更新程序,参数: " + string.Join(" ", startInfo.ArgumentList));

            if (!triggerRestartAndByEnd) return;

            LogWrapper.Info("Update", "已由于更新结束程序");
            Lifecycle.Shutdown();
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", "启动更新程序失败");
        }
    }
}