using System;
using System.IO;
using System.Threading;
using PCL.Core.Helper;
using PCL.Core.LifecycleManagement;
using PCL.Core.Utils.Logger;

namespace PCL.Core.Service;

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

    public void Start()
    {
        Context.Trace("正在初始化 Logger 实例");
        var config = new LoggerConfiguration(Path.Combine(NativeInterop.ExecutableDirectory, "PCL", "Log"));
        _logger = new Logger(config);
        Context.Trace("正在注册日志事件");
        LogWrapper.OnLog += _OnWrapperLog;
    }

    public void Stop()
    {
        LogWrapper.OnLog -= _OnWrapperLog;
        _logger?.Dispose();
    }

    private static void _OnWrapperLog(LogLevel level, string msg, string? module, Exception? ex)
    {
        var thread = Thread.CurrentThread.Name ?? $"#{Thread.CurrentThread.ManagedThreadId}";
        if (module != null) module = $"[{module}] ";
        var basic = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.PrintName()}] [{thread}] {module}";
        Logger.Log((ex == null) ? $"{basic}{msg}": $"{basic}({msg}) {ex}");
    }

    public void OnLog(LifecycleLogItem item)
    {
        Logger.Log(item.ComposeMessage());
    }
}
