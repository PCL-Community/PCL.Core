using System;
using System.Threading;
using PCL.Core.App;

namespace PCL.Core.Logging;

/// <summary>
/// 普通日志项
/// </summary>
/// <param name="message">日志消息内容</param>
/// <param name="exception">相关异常对象，若无则为 null</param>
/// <param name="level">日志等级</param>
/// <param name="actionLevel">该日志项对应的操作等级</param>
[Serializable]
public class LogItem(
    LogModule logModule,
    string message,
    Exception? exception,
    LogLevel level,
    ActionLevel? actionLevel = null)
{
    /// <summary>
    /// 创建该日志项的时间
    /// </summary>
    public DateTime Time { get; } = DateTime.Now;

    /// <summary>
    /// 创建该日志项的线程名
    /// </summary>
    public string ThreadName { get; } = Thread.CurrentThread.Name ?? $"#{Environment.CurrentManagedThreadId}";

    public LogModule LogModule { get; } = logModule;
    
    /// <summary>
    /// 日志消息内容
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// 相关异常对象，若无则为 null
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// 日志等级
    /// </summary>
    public LogLevel Level { get; } = level;

    /// <summary>
    /// 该日志项对应的操作等级
    /// </summary>
    public ActionLevel ActionLevel { get; } = actionLevel ?? level.DefaultActionLevel();

    public LogItem(
        string message,
        Exception? exception,
        LogLevel level,
        ActionLevel? actionLevel = null) : this(new LogModule(), message, exception, level, actionLevel)
    {}
        
    
    public override string ToString()
    {
        return Exception == null 
            ? $"[{Time:HH:mm:ss.fff}] {LogModule}{Message}"
            : $"[{Time:HH:mm:ss.fff}] ({LogModule}{Message}) {Exception.GetType().FullName}: {Exception.Message}";
    }

    public string ComposeMessage()
    {
        var result = $"[{Time:HH:mm:ss.fff}] [{Level.RealLevel().PrintName()}] [{ThreadName}] {LogModule}{Message}";
        if (Exception != null) result += $"\n{Exception}";
        return result;
    }
}

public class LogModule
{
    public LogModule()
    {
        _Module = null;
    }
    
    /// <summary>
    /// 使用自定义模块名创建日志模块
    /// </summary>
    /// <param name="module">源模块名</param>
    public LogModule(string module)
    {
        _Module = module;
    }
    /// <summary>
    /// 使用生命周期服务信息创建日志模块
    /// </summary>
    /// <param name="lifecycleService">源生命周期服务</param>
    public LogModule(ILifecycleService lifecycleService)
    {
        _Module = $"{lifecycleService.Name}|{lifecycleService.Identifier}";
    }

    private string? _Module { get; }

    public override string ToString() => _Module == null 
            ? " " 
            : $"[{_Module}] ";
}