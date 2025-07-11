using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using PCL.Core.Extension;
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

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = false)]
    private static extern void ExitProcess(uint statusCode);

    /// <summary>
    /// 获取当前线程的 Win32 Thread ID。若无特殊情况请用 <see cref="Thread.ManagedThreadId"/> 而不是这个方法。
    /// </summary>
    public static uint GetCurrentOsThreadId() => GetCurrentThreadId();

    /// <summary>
    /// 直接结束当前进程。若无特殊情况请使用 <see cref="PCL.Core.LifecycleManagement.Lifecycle.Shutdown"/>
    /// </summary>
    /// <param name="statusCode">退出状态码 (返回值)</param>
    public static void Exit(int statusCode = -1) => ExitProcess((uint)statusCode);

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
        threadName.Value ??= $"Worker#{thread.ManagedThreadId}";
        thread.Name = threadName.Value;
        thread.Start();
        return thread;
    }

    #endregion

    #region 命名管道通信
    
    private static void _PipeLog(string message) => LogWrapper.Trace("Pipe", message);
    private static void _PipeLogDebug(string message) => LogWrapper.Debug("Pipe", message);

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
                                _PipeLog($"[Pipe] {identifier}: 已拒绝 {clientProcessId}");
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
                        _PipeLogDebug($"{identifier}: 客户端连接已丢失");
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
                _PipeLogDebug($"{identifier}: 已断开连接");
            }

            // 释放资源并执行停止回调
            pipe.Dispose();
            _PipeLogDebug($"{identifier}: 服务端已停止");
            stopCallback?.Invoke();
        }, threadName);

        return pipe;
    }
    
    #endregion

    #region 环境信息
    
    private const string ModuleEnvironment = "Environment";
    
    public static bool ReadEnvironmentVariable<TValue>(string key, ref TValue target, bool detailLog = true)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (envValue == null) return false;
        if (detailLog) LogWrapper.Trace(ModuleEnvironment, $"环境变量 {key} 值: {envValue}");
        var value = key.Convert<TValue>();
        if (value == null) return false;
        target = value;
        LogWrapper.Debug(ModuleEnvironment, $"成功读取环境变量 {key}");
        return true;
    }
    
    /// <summary>
    /// 获取仅包含性能核（P-core）的逻辑处理器数量。
    /// 在不支持 EfficiencyClass（旧 OS 或非混合架构）时，会退回到 Environment.ProcessorCount。
    /// </summary>
    public static int GetPerformanceLogicalProcessorCount()
    {
        var cores = QueryProcessorCoreRelationships();
        if (cores.Count == 0)
        {
            // 不支持 EfficiencyClass
            return Environment.ProcessorCount;
        }
    
        // 原理：性能核的 EfficiencyClass 一定比能效核大
        var maxEff = cores.Max(c => c.EfficiencyClass);
        
        // 统计所有效率等级为 maxEff 的核心的掩码位数
        return cores
            .Where(c => c.EfficiencyClass == maxEff)
            .Sum(c => CountSetBits(c.Mask));
        
        static int CountSetBits(ulong v)
        {
            var cnt = 0;
            while (v != 0)
            {
                cnt += (int)(v & 1);
                v >>= 1;
            }
            return cnt;
        }
    }
    
    // ReSharper disable InconsistentNaming, UnusedMember.Local

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLogicalProcessorInformationEx(
        LOGICAL_PROCESSOR_RELATIONSHIP relationshipType,
        IntPtr buffer,
        ref uint returnLength);
    
    private const int ERROR_INSUFFICIENT_BUFFER = 122;
    
    private enum LOGICAL_PROCESSOR_RELATIONSHIP : uint
    {
        RelationProcessorCore    = 0,
        RelationNumaNode         = 1,
        RelationCache            = 2,
        RelationProcessorPackage = 3,
        RelationGroup            = 4,
        RelationAll              = 0xffff
    }
    
    // ReSharper restore InconsistentNaming, UnusedMember.Local

    /// <summary>
    /// 仅承载 EfficiencyClass 和 Mask 的简单 CPU 核心信息
    /// </summary>
    public sealed record ProcessorCore(byte EfficiencyClass, ulong Mask);

    /// <summary>
    /// 枚举 RelationProcessorCore 返回的所有物理核心关系信息
    /// </summary>
    // Partly generated by o4-mini-high (20250709)
    public static List<ProcessorCore> QueryProcessorCoreRelationships()
    {
        uint returnedLength = 0;

        // 第一次调用仅为了获取所需缓冲区大小
        if (!GetLogicalProcessorInformationEx(
                LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                IntPtr.Zero,
                ref returnedLength)
            && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        var list = new List<ProcessorCore>();
        IntPtr buffer = Marshal.AllocHGlobal((int)returnedLength);
        try
        {
            if (!GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                    buffer,
                    ref returnedLength))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr ptr = buffer;
            IntPtr end = IntPtr.Add(buffer, (int)returnedLength);

            // SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX 头部：Relationship (4 字节) + Size (4 字节)
            const int headerSize = sizeof(uint) + sizeof(uint);
            // GROUP_AFFINITY 大小 = KAFFINITY (平台指针大小) + WORD Group + WORD[3] Reserved
            int groupAffinitySize = IntPtr.Size + 8;

            while (ptr.ToInt64() < end.ToInt64())
            {
                uint relationship = (uint)Marshal.ReadInt32(ptr);
                uint size = (uint)Marshal.ReadInt32(ptr, sizeof(uint));

                if (relationship == (uint)LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                {
                    // PROCESSOR_RELATIONSHIP 结构：
                    // Flags                BYTE @ offset 8
                    // EfficiencyClass      BYTE @ offset 9
                    // Reserved[20]         BYTE[20]
                    // GroupCount           WORD @ offset 30
                    // GroupMask[ANYSIZE]   GROUP_AFFINITY 从 offset 32 开始

                    byte efficiencyClass = Marshal.ReadByte(ptr, headerSize + 1);
                    ushort groupCount = (ushort)Marshal.ReadInt16(ptr, headerSize + 2 + 20);
                    IntPtr maskBase = IntPtr.Add(ptr, headerSize + 2 + 20 + sizeof(ushort));

                    for (var i = 0; i < groupCount; i++)
                    {
                        var affinityPtr = IntPtr.Add(maskBase, i * groupAffinitySize);
                        // 只读取 Mask 部分，统计位数
                        var mask = (IntPtr.Size == 8 ? (ulong)Marshal.ReadInt64(affinityPtr) : (uint)Marshal.ReadInt32(affinityPtr));
                        list.Add(new ProcessorCore(efficiencyClass, mask));
                    }
                }

                // 移动到下一个记录
                ptr = IntPtr.Add(ptr, (int)size);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return list;
    }

    #endregion
}
