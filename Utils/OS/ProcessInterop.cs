using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Principal;

namespace PCL.Core.Utils.OS;

public class ProcessInterop
{
    /// <summary>
    /// 检查当前程序是否以管理员权限运行。
    /// </summary>
    /// <returns>如果当前用户具有管理员权限，则返回 true；否则返回 false。</returns>
    public static bool IsAdmin() =>
        new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    
    /// <summary>
    /// 获取指定进程 ID 的命令行参数。
    /// </summary>
    /// <param name="processId">进程 ID</param>
    /// <returns>命令行参数文本</returns>
    public static string? GetCommandLine(int processId)
    {
        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}";
        using var searcher = new ManagementObjectSearcher(query);
        return searcher.Get().GetEnumerator().Current["CommandLine"].ToString();
    }

    /// <summary>
    /// 从本地可执行文件启动新的进程。
    /// </summary>
    /// <param name="path">可执行文件路径</param>
    /// <param name="arguments">程序参数</param>
    /// <param name="runAsAdmin">指定是否以管理员身份启动该进程</param>
    /// <returns>新的进程实例</returns>
    public static Process? Start(string path, string? arguments = null, bool runAsAdmin = false)
    {
        var psi = new ProcessStartInfo(path);
        if (arguments != null) psi.Arguments = arguments;
        if (runAsAdmin) psi.Verb = "runas";
        return Process.Start(psi);
    }

    /// <summary>
    /// 获取指定进程的可执行文件路径
    /// </summary>
    /// <param name="process">进程实例</param>
    /// <returns>可执行文件路径，若无法获取则为 <c>null</c></returns>
    public static string? GetExecutablePath(Process process)
    {
        try
        {
            var path = process.MainModule?.FileName;
            return (path == null) ? null : Path.GetFullPath(path);
        }
        catch { return null; }
    }

    /// <summary>
    /// 从本地可执行文件以管理员身份启动新的进程。<see cref="Start"/> 的套壳。
    /// </summary>
    /// <param name="path">可执行文件路径</param>
    /// <param name="arguments">程序参数</param>
    /// <returns>新的进程实例</returns>
    public static Process? StartAsAdmin(string path, string? arguments = null) => Start(path, arguments, true);

    /// <summary>
    /// 结束指定进程。
    /// </summary>
    /// <param name="process">要结束的进程实例</param>
    /// <param name="timeout">等待进程退出超时，以毫秒为单位，-1 表示无限制</param>
    /// <param name="force">指定是否强制结束，若为 <c>true</c> 将通过带 <c>/F</c> 参数的 <c>TASKKILL.EXE</c> 结束进程</param>
    /// <returns>进程返回值，若等待超时将返回 <see cref="int.MinValue"/></returns>
    public static int Kill(Process process, int timeout = 3000, bool force = false)
    {
        if (force) Process.Start(new ProcessStartInfo("TASKKILL.EXE", $"/PID {process.Id} /F") { UseShellExecute = false });
        else process.Kill();
        if (timeout == -1) process.WaitForExit();
        else if (timeout != 0) process.WaitForExit(timeout);
        return process.HasExited ? process.ExitCode : int.MinValue;
    }
}
