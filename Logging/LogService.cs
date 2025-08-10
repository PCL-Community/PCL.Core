using System;
using System.IO;
using System.Threading;
using PCL.Core.App;

namespace PCL.Core.Logging;

[LifecycleService(LifecycleState.Loading, Priority = int.MaxValue)]
public class LogService : ILifecycleLogService
{
    public string Identifier => "log";
    public string Name => "日志服务";
    public bool SupportAsyncStart => false;

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private LogService() { _context = Lifecycle.GetContext(this); }
    
    private static Logger? _logger;
    public static Logger Logger => _logger!;
    
    private static bool _wrapperRegistered = false;

    public void Start()
    {
        Context.Trace("正在初始化 Logger 实例");
        var config = new LoggerConfiguration(Path.Combine(Basics.ExecutableDirectory, "PCL", "Log"));
        _logger = new Logger(config);
        Context.Trace("正在注册日志事件");
        LogWrapper.OnLog += _OnWrapperLog;
        _wrapperRegistered = true;
    }

    public void Stop()
    {
        if (_wrapperRegistered) LogWrapper.OnLog -= _OnWrapperLog;
        _logger?.Dispose();
    }

    private static void _OnWrapperLog(LogLevel level, string msg, string? module, Exception? ex)
    {
        var thread = Thread.CurrentThread.Name ?? $"#{Thread.CurrentThread.ManagedThreadId}";
        if (module != null) module = $"[{module}] ";
        msg = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.PrintName()}] [{thread}] {module}{msg}";
        Logger.Log((ex == null) ? msg : $"{msg}\n{ex}");
    }

    public void OnLog(LifecycleLogItem item)
    {
        Logger.Log(item.ComposeMessage());
    }
}
