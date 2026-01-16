using PCL.Core.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace PCL.Core.App.Updates;

public static class UpdateHelper
{
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