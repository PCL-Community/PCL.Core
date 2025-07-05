using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using PCL.Core.Utils;

namespace PCL.Core.Helper;

public static class NativeInterop
{
    #region 进程和线程
    
    /// <summary>
    /// 当前进程实例。
    /// </summary>
    public static readonly Process CurrentProcess = Process.GetCurrentProcess();
    
    /// <summary>
    /// 当前进程 ID
    /// </summary>
    public static readonly int CurrentProcessId = CurrentProcess.Id;
    
    /// <summary>
    /// 当前进程可执行文件的绝对路径。
    /// </summary>
    public static readonly string ExecutablePath = Path.GetFullPath(CurrentProcess.MainModule!.FileName);
    
    /// <summary>
    /// 当前进程可执行文件所在的目录。若有需求，请使用 <see cref="Path.Combine(string[])"/> 而不是自行拼接路径。
    /// </summary>
    public static readonly string ExecutableDirectory = Path.GetDirectoryName(ExecutablePath) ?? Path.GetPathRoot(ExecutablePath);
    
    /// <summary>
    /// 实时获取的当前目录。若要在可执行文件目录中存放文件等内容，请使用更准确的 <see cref="ExecutableDirectory"/> 而不是这个目录。
    /// </summary>
    public static string CurrentDirectory => Environment.CurrentDirectory;

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
        if (force) Process.Start("TASKKILL.EXE", $"/PID {process.Id} /F");
        else process.Kill();
        if (timeout == -1) process.WaitForExit();
        else if (timeout != 0) process.WaitForExit(timeout);
        return process.HasExited ? process.ExitCode : int.MinValue;
    }

    /// <summary>
    /// 在新的工作线程运行指定委托
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
            try
            {
                action();
            }
            catch (ThreadInterruptedException)
            {
                LogWrapper.Trace("Thread", $"{threadName.Value}: 已中止");
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Thread", $"{threadName.Value}: 抛出异常");
            }
        }) { Priority = priority };
        threadName.Value ??= $"WorkerThread@{thread.ManagedThreadId}";
        thread.Name = threadName.Value;
        thread.Start();
        return thread;
    }

    #endregion

    #region 命名管道通信
    
    private static void PipeLog(string message) => LogWrapper.Trace("Pipe", message);
    private static void PipeLogDebug(string message) => LogWrapper.Debug("Pipe", message);

    /// <summary>
    /// 获取指定命名管道当前连接的客户端进程 ID
    /// </summary>
    /// <param name="pipeHandle">命名管道句柄</param>
    /// <param name="clientProcessId">获取到的进程 ID</param>
    /// <returns>是否成功执行</returns>
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool GetNamedPipeClientProcessId(IntPtr pipeHandle, out uint clientProcessId);
    
    /// <summary>
    /// 用于命名管道通信的统一字符编码
    /// </summary>
    public static readonly Encoding PipeEncoding = Encoding.UTF8;
    
    /// <summary>
    /// 用于命名管道通信的统一终止符
    /// </summary>
    public const char PipeEndingChar = (char)27; // '\e' (ESC)
    
    /// <summary>
    /// 在新的工作线程启动命名管道服务端
    /// </summary>
    /// <param name="identifier">服务端标识，用于日志标识及工作线程的命名</param>
    /// <param name="pipeName">命名管道名称</param>
    /// <param name="loopCallback">客户端连接后的回调函数，将会提供用于读取和写入数据的流，以及客户端进程 ID，返回 <c>true</c> 表示继续等待下一个客户端连接，返回 <c>false</c> 则停止服务端运行</param>
    /// <param name="stopCallback">服务端停止后的回调函数</param>
    /// <param name="stopWhenException">指定当回调函数抛出异常时是否停止服务端运行，使用 <c>true</c> 表示停止</param>
    /// <param name="allowedProcessId">允许连接的客户端进程 ID，如为 Nothing 则允许所有</param>
    public static NamedPipeServerStream StartPipeServer(string identifier, string pipeName, Func<StreamReader, StreamWriter, Process?, bool> loopCallback, Action? stopCallback = null, bool stopWhenException = false, int[]? allowedProcessId = null)
    {
        NamedPipeServerStream pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024);
        var threadName = $"PipeServer/{identifier}";

        RunInNewThread(() =>
        {
            LogWrapper.Debug("Pipe", $"{identifier}: {pipeName} 服务端已在 '{threadName}' 工作线程启动");
            var hasNextLoop = true;
            var connected = false;

            while (hasNextLoop)
            {
                try
                {
                    hasNextLoop = false;
                    pipe.WaitForConnection(); // 等待客户端连接
                    // 获取客户端进程实例并校验
                    Process? clientProcess = null;
                    var clientProcessId = 0;
                    try
                    {
                        GetNamedPipeClientProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out var pid);
                        clientProcessId = (int)pid;
                        if (allowedProcessId != null)
                        {
                            var denied = allowedProcessId.All(id => id != clientProcessId);
                            if (denied)
                            {
                                hasNextLoop = true;
                                pipe.Disconnect();
                                PipeLog($"[Pipe] {identifier}: 已拒绝 {clientProcessId}");
                                continue;
                            }
                        }
                        clientProcess = Process.GetProcessById(clientProcessId);
                    }
                    catch (Exception)
                    {
                        if (allowedProcessId != null)
                        {
                            hasNextLoop = true;
                            throw;
                        }
                    }
                    connected = true;
                    LogWrapper.Debug("Pipe", $"{identifier}: {clientProcessId} 已连接");
                    // 初始化读取/写入流
                    var reader = new StreamReader(pipe, PipeEncoding, false, 1024, true);
                    var writer = new StreamWriter(pipe, PipeEncoding, 1024, true);
                    // 执行回调函数
                    hasNextLoop = loopCallback(reader, writer, clientProcess);
                    // 写入终止符
                    writer.Write(PipeEndingChar);
                    writer.Flush(); // 刷新写入缓冲
                    reader.Read(); // 等待客户端
                }
                catch (Exception ex)
                {
                    if (!pipe.IsConnected && connected && ex is IOException)
                    {
                        PipeLogDebug($"{identifier}: 客户端连接已丢失");
                        hasNextLoop = true;
                    }
                    else
                    {
                        LogWrapper.Warn(ex, "Pipe",  $"{identifier}: 服务端出错");
                        if (stopWhenException) hasNextLoop = false;
                    }
                }
                try
                {
                    pipe.Disconnect();
                }
                catch (InvalidOperationException)
                {
                    // 由于没妈的巨硬给的 IsConnected 不一定是准确的，需要运行 Disconnect() 确保管道断开连接
                    // 如果已经断开会抛出 InvalidOperationException 这里直接忽略掉
                }
                connected = false;
                PipeLogDebug($"{identifier}: 已断开连接");
            }

            // 释放资源并执行停止回调
            pipe.Dispose();
            PipeLogDebug($"{identifier}: 服务端已停止");
            stopCallback?.Invoke();
        }, threadName);

        return pipe;
    }
    
    #endregion
}
