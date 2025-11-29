using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using PCL.Core.Logging;

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
    
    public static void Restart(bool triggerRestartAndByEnd)
    {
        try
        {
            var fileName = Path.GetFullPath(Path.Combine(Basics.ExecutableDirectory, "PCL", "Plain Craft Launcher Community Edition.exe"));

            if (!File.Exists(fileName))
            {
                LogWrapper.Warn("System", "更新启动器文件不存在，无法启动更新程序");
                return;
            }

            var args = $"update {Environment.ProcessId} \"{Basics.ExecutablePath}\" \"{fileName}\" true";

            var startInfo = new ProcessStartInfo(fileName)
            {
                Arguments = args,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            LogWrapper.Info("System", "已启动更新程序,参数: " + args);

            if (!triggerRestartAndByEnd) return;
            
            LogWrapper.Info("System", "已由于更新强制结束程序");
            Environment.Exit(0);
        }
        catch (Win32Exception)
        {
            // 被拦截或权限问题：保持与原实现一致，静默处理或在外部记录
        }
        catch
        {
            // 忽略其他启动异常以保持方法简洁（可按需记录）
        }
    }
}

public enum UpdateChannel
{
    Stable,
    Beta
}
