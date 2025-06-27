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
    public bool SupportAsyncStart => true;
    
    private readonly LifecycleContext Context;
    private LogService() { Context = Lifecycle.GetContext(this); }
    
    private static Logger? _logger;
    public static Logger Logger { get => _logger!; private set => _logger = value; }
    
    public void Start()
    {
        Context.Trace("正在初始化 Logger 实例");
        var config = new LoggerConfiguration(Path.Combine(Environment.CurrentDirectory, "PCL", "Log"));
        Logger = new Logger(config);
        Context.Trace("正在注册日志事件");
        LogWrapper.OnLog += OnWrapperLog;
    }

    public void Stop()
    {
        Context.Trace("正在取消注册日志事件");
        LogWrapper.OnLog -= OnWrapperLog;
        Context.Trace("正在结束 Logger 实例");
        Logger.Dispose();
    }

    private void OnWrapperLog(LogLevel level, string msg, string? module, Exception? ex)
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
