﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

/// <summary>
/// 启动器生命周期管理
/// </summary>
[LifecycleService(LifecycleState.BeforeLoading, Priority = int.MaxValue)]
public sealed class Lifecycle : ILifecycleService
{
    public string Identifier => "lifecycle";
    public string Name => "生命周期";
    public bool SupportAsyncStart => false;
    
    private static LifecycleContext? _context;
    private Lifecycle() { _context = GetContext(this); }
    private static LifecycleContext Context => _context ?? System;
    
    public void Start() { }
    public void Stop() { _context = null; }

    // -- 日志管理 --

    private static ILifecycleLogService? _logService;
    private static readonly List<LifecycleLogItem> _PendingLogs = [];

    private static void _PushLog(LifecycleLogItem item, ILifecycleLogService service)
    {
        service.OnLog(item);
    }

    public static string PendingLogDirectory { get; set; } = @"PCL\Log";
    public static string PendingLogFileName { get; set; } = "LastPending.log";

    private static void _SavePendingLogs()
    {
        if (_PendingLogs.Count == 0)
        {
            Console.WriteLine("[Lifecycle] No pending logs");
            return;
        }
        try
        {
            // 直接写入剩余未输出日志到程序目录
            var path = Path.Combine(PendingLogDirectory, PendingLogFileName);
            if (!Path.IsPathRooted(path)) path = Path.Combine(Basics.ExecutableDirectory, path);
            Directory.CreateDirectory(Basics.GetParentPathOrDefault(path));
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (var item in _PendingLogs) writer.WriteLine(item.ComposeMessage());
            Console.WriteLine($"[Lifecycle] Pending logs saved to {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("[Lifecycle] Error saving pending logs, writing to stdout...");
            foreach (var item in _PendingLogs) Console.WriteLine(item.ComposeMessage());
        }
    }

    // -- 服务管理 --

    private static readonly Dictionary<string, LifecycleServiceInfo> _RunningServiceInfoMap = [];
    private static readonly LinkedList<ILifecycleService> _RunningServiceList = [];
    private static readonly Dictionary<string, ILifecycleService> _ManualServiceMap = [];
    private static readonly HashSet<ILifecycleService> _DeclaredStoppedServices = [];

    private static string _ServiceName(ILifecycleService service, LifecycleState? state = null)
    {
#if DEBUG
        var info = GetServiceInfo(service.Identifier);
        if (info != null) state = info.StartState;
        var stateText = (state == null) ? "" : $"{state}/";
        return $"{service.Name} ({stateText}{service.Identifier})";
#else
        return service.Name;
#endif
    }

    private static void _StartService(ILifecycleService service, bool manual = false)
    {
        ILifecycleLogService? logService = null;
        // 检测日志服务
        if (service is ILifecycleLogService ls)
        {
            if (_logService != null) throw new InvalidOperationException("日志服务只能有一个");
            logService = ls;
        }
        var state = manual ? LifecycleState.Manual : CurrentState;
        var name = _ServiceName(service, state);
        // 确保不存在重复的标识符
        lock (_ManualServiceMap) {
            if (_ManualServiceMap.ContainsKey(service.Identifier) && IsServiceRunning(service.Identifier))
            {
                Context.Warn($"{name} 标识符重复，已跳过");
                return;
            }
            // 先找个东西占着防止异步加载中检测逻辑失效
            _RunningServiceInfoMap[service.Identifier] = _SystemServiceInfo;
        }
        // 运行服务项并添加到正在运行列表
        try
        {
            Context.Trace($"正在启动 {name}");
            var serviceInfo = new LifecycleServiceInfo(service, state);
            Context.Debug($"{name} 启动成功");
            if (_DeclaredStoppedServices.Contains(service))
            {
                _DeclaredStoppedServices.Remove(service);
                Context.Trace($"{name} 主动停止");
            }
            else
            {
                // 若该服务未声明自己已结束运行，将其添加到正在运行列表
                _RunningServiceList.AddFirst(service);
                _RunningServiceInfoMap[service.Identifier] = serviceInfo;
            }
        }
        catch (Exception ex)
        {
            Context.Warn($"{name} 启动失败，尝试停止", ex);
            _StopService(service, false);
        }
        // 若日志服务已启动则清空日志缓冲
        if (logService == null) return;
        lock (_PendingLogs)
        {
            foreach (var item in _PendingLogs) _PushLog(item, logService);
            _PendingLogs.Clear();
            _logService = logService;
        }
    }

    private static Type[] _GetServiceTypes(LifecycleState state) => LifecycleServiceTypes.GetServiceTypes(state);

    private static ILifecycleService _CreateService(Type type)
    {
        var fullname = type.FullName;
        try
        {
            System.Trace($"正在实例化 {fullname}");
            var instance = (ILifecycleService)Activator.CreateInstance(type, true)!;
            var supportAsyncText = instance.SupportAsyncStart ? "异步" : "同步";
            System.Trace($"实例化完成: {instance.Name} ({instance.Identifier}), 启动方式: {supportAsyncText}");
            return instance;
        }
        catch (Exception ex)
        {
            System.Fatal($"注册服务项实例化失败: {fullname}", ex);
            throw;
        }
    }

    private static void _LogStateCount(TimeSpan count, LifecycleState state)
    {
        Context.Debug($"状态 {state} 共用时 {Math.Round(count.TotalMilliseconds)} ms");
    }

    private static void _InitializeAndStartStateServices(LifecycleState state)
    {
        var types = _GetServiceTypes(state);
        if (types.Length == 0) return; // 跳过空列表
        var asyncInstances = new List<ILifecycleService>();
        // 运行非异步启动服务并存储异步启动服务
        foreach (var service in types)
        {
            var instance = _CreateService(service);
            if (instance.SupportAsyncStart) asyncInstances.Add(instance);
            else _StartService(instance);
            if (_requestedStopLoading) return; // 若请求停止加载则提前结束
        }
        // 运行异步启动服务并等待所有服务启动完成
        var taskList = asyncInstances.Select(
            instance => Task.Run(() => _StartService(instance))).ToArray();
        Task.WaitAll(taskList);
    }

    private static void _StartStateFlow(LifecycleState start, LifecycleState? end = null, bool count = true)
    {
        var index = (int)start;
        var endIndex = end == null ? index : (int)end;
        while (index <= endIndex)
        {
            DateTime? countStart = count ? DateTime.Now : null; //开始计时
            var state = (LifecycleState)index;
            _NextState(state);
            _InitializeAndStartStateServices(state);
            if (countStart is { } s)
            {
                var countSpan = DateTime.Now - s; // 结束计时
                _LogStateCount(countSpan, state);
            }
            index++;
        }
    }

    private static void _RemoveRunningInstance(ILifecycleService service)
    {
        _RunningServiceInfoMap.Remove(service.Identifier);
        _RunningServiceList.Remove(service);
    }

    private static void _StopService(ILifecycleService service, bool async, bool manual = false)
    {
        var name = _ServiceName(service, manual ? LifecycleState.Manual : CurrentState);
        if (async) Task.Run(Stop);
        else Stop();
        return;

        void Stop()
        {
            try
            {
                Context.Trace($"正在停止 {name}");
                service.Stop();
                Context.Debug($"{name} 已停止");
            }
            catch (Exception ex)
            {
                // 若出错直接忽略
                Context.Warn($"停止 {name} 时出错，已跳过", ex);
            }
            // 从正在运行列表移除
            _RemoveRunningInstance(service);
        }
    }

    private static void _RunCurrentExecutable(string? arguments)
    {
        var fileName = Process.GetCurrentProcess().MainModule!.FileName;
        if (arguments == null) Process.Start(fileName);
        else Process.Start(fileName, arguments);
    }

    private static bool _hasRequestedRestart = false;
    private static string? _requestRestartArguments;
    private static ILifecycleService? _requestRestartService;

    private static void _Exit(int statusCode = 0)
    {
        if (HasShutdownStarted) return;
        HasShutdownStarted = true;
        // 结束 Running 计时
        if (_countRunningStart is { } start)
        {
            var countSpan = DateTime.Now - start;
            _LogStateCount(countSpan, LifecycleState.Running);
        }
        // 开始 Exiting 状态
        _StartStateFlow(LifecycleState.Exiting, count: false);
        // 停止服务
        Context.Debug("正在停止运行中的服务");
        ILifecycleLogService? logService = null;
        foreach (var service in _RunningServiceList.ToArray())
        {
            if (service is ILifecycleLogService ls)
            {
                // 跳过日志服务
                Context.Trace($"已跳过日志服务: {_ServiceName(ls)}");
                logService = ls;
                continue;
            }
            _StopService(service, service.SupportAsyncStart);
        }
        if (logService != null)
        {
            Context.Trace($"退出过程已结束，正在停止日志服务");
            // 直接调用 Stop() 不使用常规停止实现 以保证正常情况下不会向等待区输出日志
            logService.Stop();
            Console.WriteLine("[Lifecycle] Log service stopped");
        }
        _SavePendingLogs();
#if TRACE
        // 输出仍在运行的线程
        Console.WriteLine("[Lifecycle] Thread(s) still in working:");
        var currentThreadId = KernelInterop.CurrentNativeThreadId;
        foreach (ProcessThread processThread in Process.GetCurrentProcess().Threads)
        {
            var threadId = processThread.Id;
            if (threadId == currentThreadId) continue;
            Console.WriteLine($" - {threadId}({processThread.ThreadState}) (Start from {processThread.StartTime})");
        }
#endif
        if (_hasRequestedRestart && _requestRestartService is { } s)
        {
            Console.WriteLine($"[Lifecycle] Requested by '{s.Identifier}', restarting the program...");
            _RunCurrentExecutable(_requestRestartArguments);
        }
        // 退出程序
        Console.WriteLine($"[Lifecycle] Exiting program with status: {statusCode}");
        // 执行正常退出
        Environment.Exit(statusCode);
        // 保险起见，只要运行环境正常根本不可能执行到这里，但是永远都不能假设用户的环境是正常的
        Console.WriteLine("[Lifecycle] Warning! Abnormal behaviour, try to kill process 1s later.");
        Thread.Sleep(1000);
        _KillCurrentProcess();
    }

    private static void _FatalExit()
    {
        ForceShutdown(-1);
    }

    private static void _KillCurrentProcess()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "taskkill.exe",
            Arguments = $"/f /t /pid {Environment.ProcessId}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }

    private static void _StartWorker(LifecycleState state, LifecycleState? wait = null, bool count = true)
    {
        new Thread(() =>
        {
            _StartStateFlow(state, count: count);
            if (wait is { } w) WaitForState(w);
        })
        { IsBackground = true, Name = $"Lifecycle/{state}" }.Start();
    }

    // -- 状态控制 --

    private static LifecycleState _currentState = LifecycleState.BeforeLoading;

    private static void _NextState(LifecycleState? enforce = null)
    {
        if (enforce is { } state) CurrentState = state;
        else CurrentState++;
    }

    /// <summary>
    /// 生命周期状态改变时触发的事件。<br/>
    /// <b>非异步执行，请注意自行实现必要的异步，否则会卡住生命周期管理线程。</b>
    /// </summary>
    public static event Action<LifecycleState>? StateChanged;

    /// <summary>
    /// 阻塞当前线程并等待到达指定生命周期状态。
    /// </summary>
    /// <param name="state">指定生命周期状态</param>
    /// <returns>
    /// 是否真正“等待”过（若调用该方法时已经到达或晚于指定状态，则为 <c>false</c>）
    /// </returns>
    public static bool WaitForState(LifecycleState state)
    {
        if (CurrentState >= state) return false; // 如果已经是目标状态，直接返回
        using var mre = new ManualResetEventSlim(false);
        StateChanged += TempHandler;
        try { mre.Wait(); } // 等待 Set() 方法
        finally { StateChanged -= TempHandler; } // 取消订阅，避免内存泄漏或重复唤醒
        return true;

        void TempHandler(LifecycleState s)
        {
            // ReSharper disable once AccessToDisposedClosure
            if (s == state) mre.Set();
        }
    }

    /// <summary>
    /// 异步等待到达指定生命周期状态。
    /// </summary>
    /// <param name="state">指定生命周期状态</param>
    /// <returns>
    /// 结果表示是否真正“等待”过的 <see cref="Task"/> 实例（若调用该方法时已经到达或晚于指定状态，则结果为 <c>false</c>）
    /// </returns>
    public static Task<bool> WaitForStateAsync(LifecycleState state)
    {
        if (CurrentState >= state) return Task.FromResult(false); // 如果已经是目标状态，则直接返回 false
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StateChanged += TempHandler;
        return tcs.Task;

        void TempHandler(LifecycleState s)
        {
            if (s != state) return;
            StateChanged -= TempHandler;
            tcs.TrySetResult(true);
        }
    }

    /// <summary>
    /// 快速注册改变到目标生命周期状态的事件，与直接注册 <see cref="StateChanged"/> 的区别是会自动判断目标状态并自动移除事件注册。
    /// </summary>
    /// <param name="when">目标生命周期状态</param>
    /// <param name="action">事件触发委托</param>
    public static void When(LifecycleState when, Action action)
    {
        if (CurrentState >= when) return;
        StateChanged += TempHandler;
        return;

        void TempHandler(LifecycleState state)
        {
            if (state != when) return;
            action();
            StateChanged -= TempHandler;
        }
    }

    // -- 流程触发 --

    private static DateTime? _countRunningStart;

    private static bool _isApplicationStarted = false;
    private static bool _isLoadingStarted = false;
    private static bool _isWindowCreated = false;

    private static bool _requestedStopLoading = false;

    /// <summary>
    /// [请勿调用] 处理未捕获异常流程
    /// </summary>
    /// <param name="ex">异常对象</param>
    public static void OnException(object ex)
    {
        Context.Fatal("未捕获的异常", ex as Exception);
    }

    /// <summary>
    /// [请勿调用] 程序初始化流程
    /// </summary>
    public static void OnInitialize()
    {
        // 检测重复调用
        if (_isApplicationStarted) return;
        _isApplicationStarted = true;
        // 修改 STA 线程名
        Thread.CurrentThread.Name = "STA";
        // 注册全局事件
        AppDomain.CurrentDomain.UnhandledException += (_, e) => OnException(e.ExceptionObject);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => _Exit();
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Context.Error("未观测到的异步任务异常", e.Exception);
            e.SetObserved();
        };
        // 添加系统服务
        _RunningServiceInfoMap["system"] = _SystemServiceInfo;
        // 实例化并存储手动服务
        foreach (var service in _GetServiceTypes(LifecycleState.Manual))
        {
            var instance = _CreateService(service);
            var identifier = instance.Identifier;
            if (_ManualServiceMap.TryAdd(identifier, instance)) continue;
            Context.Warn($"{_ServiceName(instance, LifecycleState.Manual)} 标识符重复，已跳过");
        }
        // 运行预加载服务
        _StartStateFlow(LifecycleState.BeforeLoading);
        if (_requestedStopLoading) return;
        // 运行应用程序容器
        var statusCode = CurrentApplication.Run();
        if (!HasShutdownStarted) _Exit(statusCode);
    }

    /// <summary>
    /// [请勿调用] 组件加载流程
    /// </summary>
    public static void OnLoading()
    {
        // 检测重复调用
        if (_isLoadingStarted) return;
        _isLoadingStarted = true;
        // 运行加载阶段服务
        _StartStateFlow(LifecycleState.Loading, LifecycleState.WindowCreating);
        // 运行窗体
        CurrentApplication.MainWindow!.Show();
    }

    /// <summary>
    /// [请勿调用] 窗口创建结束流程
    /// </summary>
    public static void OnWindowCreated()
    {
        // 检测重复调用
        if (_isWindowCreated) return;
        _isWindowCreated = true;
        // 启动窗口流程后的服务项
        _StartStateFlow(LifecycleState.WindowCreated);
        _countRunningStart = DateTime.Now;
        _StartWorker(LifecycleState.Running, LifecycleState.Exiting, false);
    }

    /// <summary>
    /// [请勿调用] 尝试结束程序流程 (未实现)
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public static void OnWindowClosing()
    {
        // TODO 尝试退出程序 (Closing)
        throw new NotImplementedException();
    }

    // -- 其余公共成员 --

    /// <summary>
    /// 当前的生命周期状态，会随生命周期变化随时更新。
    /// </summary>
    public static LifecycleState CurrentState
    {
        get => _currentState;
        private set
        {
            Context.Debug($"状态改变: {value}");
            _currentState = value;
            try
            {
                StateChanged?.Invoke(value);
            }
            catch (Exception ex)
            {
                Context.Warn($"状态更改事件出错", ex);
            }
        }
    }

    private static Application? _currentApplication;
    public static Application CurrentApplication { get => _currentApplication!; set => _currentApplication = value; }

    /// <summary>
    /// 日志服务启动状态
    /// </summary>
    public static bool IsLogServiceStarted => _logService != null;

    /// <summary>
    /// 是否正在关闭程序
    /// </summary>
    public static bool HasShutdownStarted { get; private set; } = false;

    /// <summary>
    /// 正在进行的关闭程序流程是否是强制关闭
    /// </summary>
    public static bool IsForceShutdown { get; private set; } = false;

    /// <summary>
    /// 所有正在运行的服务项标识符（即 <see cref="ILifecycleService.Identifier"/> 属性）
    /// </summary>
    public static ICollection<string> RunningServices => _RunningServiceInfoMap.Keys;

    /// <summary>
    /// 检查指定标识符的服务项是否正在运行
    /// </summary>
    /// <param name="identifier">服务项标识符（即 <see cref="ILifecycleService.Identifier"/> 属性）</param>
    /// <returns>服务项是否正在运行</returns>
    public static bool IsServiceRunning(string identifier) => _RunningServiceInfoMap.ContainsKey(identifier);

    /// <summary>
    /// 根据标识符获取正在运行的服务项的相关信息
    /// </summary>
    /// <param name="identifier">服务项标识符（即 <see cref="ILifecycleService.Identifier"/> 属性）</param>
    /// <returns>服务项信息</returns>
    public static LifecycleServiceInfo? GetServiceInfo(string? identifier)
    {
        if (identifier == null) return null;
        _RunningServiceInfoMap.TryGetValue(identifier, out var info);
        return info;
    }

    /// <summary>
    /// 手动请求启动一个周期为 <see cref="LifecycleState.Manual"/> 的服务项。
    /// </summary>
    /// <param name="identifier">服务项标识符（即 <see cref="ILifecycleService.Identifier"/> 属性）</param>
    /// <param name="async">是否异步启动，默认遵循服务项自身的声明</param>
    /// <returns>是否成功请求启动，若该服务项正在运行或周期不是 <see cref="LifecycleState.Manual"/> 则无法启动</returns>
    public static bool StartService(string identifier, bool? async = null)
    {
        _ManualServiceMap.TryGetValue(identifier, out var service);
        if (service == null || IsServiceRunning(identifier)) return false;
        async ??= service.SupportAsyncStart;
        if (async == true) Task.Run(() => _StartService(service, true));
        else _StartService(service, true);
        return true;
    }

    /// <summary>
    /// 手动请求停止一个周期为 <see cref="LifecycleState.Manual"/> 的服务项。
    /// </summary>
    /// <param name="identifier">服务项标识符（即 <see cref="ILifecycleService.Identifier"/> 属性）</param>
    /// <param name="async">是否异步停止，默认为 <c>true</c></param>
    /// <returns>是否成功请求停止，若该服务项未运行或周期不是 <see cref="LifecycleState.Manual"/> 则无法停止</returns>
    public static bool StopService(string identifier, bool async = true)
    {
        _ManualServiceMap.TryGetValue(identifier, out var service);
        if (service == null || !IsServiceRunning(identifier)) return false;
        _StopService(service, async, true);
        return true;
    }

    /// <summary>
    /// 运行自定义服务项。该服务项将使用当前生命周期状态作为启动状态，若无特殊需求请尽可能不要使用，而是直接注册服务项。
    /// </summary>
    /// <param name="service">服务项实例</param>
    /// <returns>是否成功请求运行，若标识符与正在运行的服务或已注册的手动服务冲突则无法运行。</returns>
    public static bool StartCustomService(ILifecycleService service)
    {
        if (IsServiceRunning(service.Identifier) || _ManualServiceMap.ContainsKey(service.Identifier)) return false;
        _StartService(service);
        return true;
    }

    /// <summary>
    /// 发起关闭程序流程。<br/>
    /// <see cref="LifecycleState.BeforeLoading"/> 状态请使用 <see cref="LifecycleContext.RequestExit"/>。
    /// </summary>
    /// <param name="statusCode">退出状态码 (返回值)</param>
    /// <param name="force">指定是否强制关闭，即不执行 WPF 标准关闭流程</param>
    /// <exception cref="InvalidOperationException">尝试在 <see cref="LifecycleState.BeforeLoading"/> 状态调用</exception>
    public static void Shutdown(int statusCode = 0, bool force = false)
    {
        if (CurrentState == LifecycleState.BeforeLoading) throw new InvalidOperationException();
        if (HasShutdownStarted) return;
        Context.Info(force ? "开始强制关闭程序" : "正在关闭程序");
        IsForceShutdown = force;
        if (force) _Exit(statusCode);
        else new Thread(() => _Exit(statusCode)) { Name = "Shutdown" }.Start();
    }

    /// <summary>
    /// 强制关闭程序，不执行 WPF 标准关闭流程。
    /// </summary>
    /// <param name="statusCode">退出状态码 (返回值)</param>
    /// <exception cref="InvalidOperationException">尝试在 <see cref="LifecycleState.BeforeLoading"/> 时调用</exception>
    public static void ForceShutdown(int statusCode = 0) => Shutdown(statusCode, true);

    /// <summary>
    /// 获取指定服务项对应的上下文实例用于日志输出、多任务通信等。一般情况下只推荐获取自身上下文。
    /// </summary>
    /// <param name="self">服务项实例</param>
    /// <returns>上下文实例</returns>
    public static LifecycleContext GetContext(ILifecycleService self) => new(
        service: self,
        onLog: item =>
        {
            lock (_PendingLogs)
            {
                if (_logService == null) _PendingLogs.Add(item);
                else _PushLog(item, _logService);
            }
            if (item.ActionLevel == ActionLevel.MsgBoxFatal) _FatalExit();
        },
        onRequestExit: statusCode =>
        {
            if (CurrentState != LifecycleState.BeforeLoading)
                throw new InvalidOperationException("只能在 BeforeLoading 时请求退出");
            Context.Info($"{_ServiceName(self)} 已请求退出程序");
            _Exit(statusCode);
        },
        onRequestRestart: args =>
        {
            _hasRequestedRestart = true;
            _requestRestartService = self;
            _requestRestartArguments = args;
        },
        onDeclareStopped: () =>
        {
            var identifier = self.Identifier;
            if (GetServiceInfo(identifier)?.Identifier == identifier)
                throw new InvalidOperationException("只能在服务启动阶段调用");
            _DeclaredStoppedServices.Add(self);
        },
        onRequestStopLoading: () =>
        {
            if (CurrentState != LifecycleState.BeforeLoading)
                throw new InvalidOperationException("只能在 BeforeLoading 时请求停止加载");
            Context.Info($"{_ServiceName(self)} 已请求停止继续加载");
            _requestedStopLoading = true;
        }
    );

    private class SystemLifecycleService : ILifecycleService
    {
        public string Name => "系统";
        public string Identifier => "system";
        public bool SupportAsyncStart => false;
        public void Start() { }
        public void Stop() { }
    }

    private static readonly ILifecycleService _SystemService = new SystemLifecycleService();
    private static readonly LifecycleServiceInfo _SystemServiceInfo = new(_SystemService, LifecycleState.BeforeLoading);

    /// <summary>
    /// 系统默认上下文，无特殊需求请勿使用。
    /// </summary>
    public static readonly LifecycleContext System = GetContext(_SystemService);
}
