using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using PCL.Core.Logging;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

public static class Basics
{
    /// <summary>
    /// 当前进程实例。
    /// </summary>
    public static readonly Process CurrentProcess = Process.GetCurrentProcess();

    /// <summary>
    /// 当前进程 ID。
    /// </summary>
    public static readonly int CurrentProcessId = CurrentProcess.Id;

    /// <summary>
    /// 当前进程可执行文件的绝对路径。
    /// </summary>
    public static readonly string ExecutablePath = ProcessInterop.GetExecutablePath(CurrentProcess)!;

    /// <summary>
    /// 当前进程可执行文件所在的目录。若有需求，请使用 <see cref="Path.Combine(string[])"/> 而不是自行拼接路径。
    /// </summary>
    public static readonly string ExecutableDirectory = GetParentPath(ExecutablePath) ?? CurrentDirectory;

    /// <summary>
    /// 当前进程可执行文件的名称，含扩展名。
    /// </summary>
    public static readonly string ExecutableName = Path.GetFileName(ExecutablePath);

    /// <summary>
    /// 当前进程可执行文件的名称，不含扩展名。
    /// </summary>
    public static readonly string ExecutableNameWithoutExtension = Path.GetFileNameWithoutExtension(ExecutablePath);

    /// <summary>
    /// 当前进程不包括第一个参数（文件名）的命令行参数。
    /// </summary>
    public static readonly string[] CommandLineArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

    /// <summary>
    /// 实时获取的当前目录。若要在可执行文件目录中存放文件等内容，请使用更准确的 <see cref="ExecutableDirectory"/> 而不是这个目录。
    /// </summary>
    public static string CurrentDirectory => Environment.CurrentDirectory;

    /// <summary>
    /// 在新的工作线程运行指定委托。
    /// </summary>
    /// <param name="action">要运行的委托</param>
    /// <param name="name">线程名，默认为 <c>WorkerThread@[ThreadId]</c></param>
    /// <param name="priority">线程优先级</param>
    /// <returns>新创建的线程实例</returns>
    public static Thread RunInNewThread(Action action, string? name = null, ThreadPriority priority = ThreadPriority.Normal)
    {
        var threadName = new AtomicVariable<string>(name);
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (ThreadInterruptedException) { LogWrapper.Trace("Thread", $"{threadName.Value}: 已中止"); }
            catch (Exception ex) { LogWrapper.Error(ex, "Thread", $"{threadName.Value}: 抛出异常"); }
        }) { Priority = priority };
        threadName.Value ??= $"Worker#{thread.ManagedThreadId}";
        thread.Name = threadName.Value;
        thread.Start();
        return thread;
    }

    /// <summary>
    /// 获取某个路径的父路径/目录。
    /// </summary>
    /// <param name="path">路径文本</param>
    /// <returns>父路径文本，可能为 <c>null</c></returns>
    public static string? GetParentPath(string path) => Path.GetDirectoryName(path) ?? Path.GetPathRoot(path);

    /// <summary>
    /// 获取某个路径的父路径/目录。
    /// </summary>
    /// <param name="path">路径文本</param>
    /// <returns>父路径文本，或空白</returns>
    public static string GetParentPathOrEmpty(string path) => GetParentPath(path) ?? string.Empty;

    /// <summary>
    /// 获取某个路径的父路径/目录。
    /// </summary>
    /// <param name="path">路径文本</param>
    /// <returns>父路径文本，或默认 (<see cref="CurrentDirectory"/>)</returns>
    public static string GetParentPathOrDefault(string path) => GetParentPath(path) ?? CurrentDirectory;
}
