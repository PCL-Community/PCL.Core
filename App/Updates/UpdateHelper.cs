using PCL.Core.Logging;
using PCL.Core.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Models;

namespace PCL.Core.App.Updates;

public static class UpdateHelper
{
    /// <summary>
    /// 更新启动器（替换文件）
    /// </summary>
    /// <param name="source">用于替换的来源文件路径</param>
    /// <param name="target">目标文件路径</param>
    /// <exception cref="FileNotFoundException">Throws if backup file not found</exception>
    public static async Task<Exception?> ReplaceAsync(string source, string target)
    {
        var backup = $"{target}.bak.{DateTime.Now:yyyyMMddHHmmss}";
        Exception? lastEx = null;
        try
        {
            // get full file path. e.g. /example/ex.txt -> C:/example/ex.txt
            source = Path.GetFullPath(source);
            target = Path.GetFullPath(target);

            // 备份目标文件
            File.Copy(target, backup);
            if (!File.Exists(backup))
            {
                throw new FileNotFoundException("备份目标文件失败", backup);
            }

            // delete origin file
            await FileDeleteHelper.DeleteFileAndWaitAsync(target).ConfigureAwait(false);

            // 复制到目标文件
            File.Copy(source, target);
            if (!File.Exists(target))
            {
                throw new FileNotFoundException("复制到目标文件失败", target);
            }
        }
        catch (IOException ex)
        {
            // 出错：恢复原文件并返回异常（文件 I/O 相关）
            if (File.Exists(backup) && !File.Exists(target))
            {
                File.Move(backup, target);
            }

            lastEx = ex;
        }
        catch (UnauthorizedAccessException ex)
        {
            // 出错：恢复原文件并返回异常（权限相关）
            if (File.Exists(backup) && !File.Exists(target))
            {
                File.Move(backup, target);
            }
            lastEx = ex;
        }
        catch (SystemException ex)
        {
            // 其他非致命系统异常：尝试恢复并返回异常
            if (File.Exists(backup) && !File.Exists(target))
            {
                File.Move(backup, target);
            }

            lastEx = ex;
        }

        if (File.Exists(backup))
        {
            File.Delete(backup);
        }

        return lastEx;
    }

    // ReSharper disable once FlagArgument
    public static void Restart(bool triggerRestartAndByEnd)
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
                    "true"
                },
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
            LogWrapper.Info("Update", "已尝试启动更新程序,参数: " + string.Join(" ", startInfo.ArgumentList));

            if (!triggerRestartAndByEnd) return;

            LogWrapper.Info("Update", "已由于更新强制结束程序");
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
    
    /// <summary>
    /// 比较两个版本。
    /// </summary>
    /// <param name="v1">第一个版本</param>
    /// <param name="v1Code">第一个版本号</param>
    /// <param name="v2">第二个版本</param>
    /// <param name="v2Code">第二个版本号</param>
    /// <returns>比较结果：小于 0 表示 v1 小于 v2，大于 0 表示 v1 大于 v2，等于 0 表示两者相等。</returns>
    public static int CompareVersion(SemVer v1, int v1Code, SemVer v2, int v2Code)
    {
        var codeComparison = v1Code.CompareTo(v2Code);
        return codeComparison != 0 ? codeComparison : v1.CompareTo(v2);
    }
    
    /// <summary>
    /// 当前更新通道。
    /// </summary>
    public static UpdateChannel CurrentUpdateChannel => (UpdateChannel)Config.System.UpdateBranch;

    /// <summary>
    /// 当前系统架构是否为 Arm64。
    /// </summary>
    public static bool IsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}