using System;
using System.Collections.Generic;
using System.Threading;
using PCL.Core.App;

namespace PCL.Core.Logging;

public delegate void LogHandler(LogLevel level, string msg, string? module = null, Exception? ex = null);

public static class LogWrapper
{
    public static readonly List<LogItem> PendingLogs = [];
    
    // Fatal: can handle exceptions
    public static void Fatal(Exception? ex, string? module, string msg) => _LogAction(LogLevel.Fatal, msg, module, ex);
    public static void Fatal(Exception? ex, string msg) => Fatal(ex, null, msg);
    public static void Fatal(string? module, string msg) => Fatal(null, module, msg);
    public static void Fatal(string msg) => Fatal((string?)null, msg);
    
    // Error: can handle exceptions
    public static void Error(Exception? ex, string? module, string msg) => _LogAction(LogLevel.Error, msg, module, ex);
    public static void Error(Exception? ex, string msg) => Error(ex, null, msg);
    public static void Error(string? module, string msg) => Error(null, module, msg);
    public static void Error(string msg) => Error((string?)null, msg);
    
    // Warn: can handle exceptions
    public static void Warn(Exception? ex, string? module, string msg) => _LogAction(LogLevel.Warning, msg, module, ex);
    public static void Warn(Exception? ex, string msg) => Warn(ex, null, msg);
    public static void Warn(string? module, string msg) => Warn(null, module, msg);
    public static void Warn(string msg) => Warn((string?)null, msg);
    
    // Info
    public static void Info(string? module, string msg) => _LogAction(LogLevel.Info, msg, module);
    public static void Info(string msg) => Info(null, msg);

    // Debug
    public static void Debug(string? module, string msg) => _LogAction(LogLevel.Debug, msg, module);
    public static void Debug(string msg) => Debug(null, msg);

    // Trace
    public static void Trace(string? module, string msg) => _LogAction(LogLevel.Trace, msg, module);
    public static void Trace(string msg) => Trace(null, msg);

    public static ILifecycleLogService? CurrentLogService
    {
        get;
        set
        {
            field = value;
            if (PendingLogs.Count != 0)
            {
                PendingLogs.ForEach(item => field?.OnLog(item));
            }
        }
    }

    public static Logger CurrentLogger => LogService.Logger;

    private static void _LogAction(LogLevel level, string msg, string? module = null, Exception? ex = null)
    {
        if (module == null)
        {
            PushLog(new LogItem(msg, ex, level, level.DefaultActionLevel()));
            return;
        }
        PushLog(new LogItem(module, msg, ex, level, level.DefaultActionLevel()));
    }
    
    public static void PushLog(LogItem item)
    {
        if (CurrentLogService != null)
        {
            CurrentLogService.OnLog(item);
            return;
        }

        PendingLogs.Add(item);
    }
}

/// <summary>
/// 日志项
/// </summary>
[Serializable]
public class LogItem(
    string message,
    Exception? exception,
    LogLevel level,
    ActionLevel? actionLevel)
{
    /// <summary>
    /// 创建该日志项的时间
    /// </summary>
    public DateTime Time { get; } = DateTime.Now;

    /// <summary>
    /// 创建该日志项的线程名
    /// </summary>
    public string ThreadName { get; } = Thread.CurrentThread.Name ?? $"#{Environment.CurrentManagedThreadId}";
    
    /// <summary>
    /// 日志消息内容 (不包含时间戳和线程名, 包含模块, 生命周期服务等信息)
    /// </summary>
    public string Message { get; init; } = message;

    /// <summary>
    /// 相关异常对象，若无则为 null
    /// </summary>
    public Exception? Exception { get; init; } = exception;

    /// <summary>
    /// 日志等级
    /// </summary>
    public LogLevel Level { get; init; } = level;

    /// <summary>
    /// 该日志项对应的操作等级
    /// </summary>
    public ActionLevel ActionLevel { get; init; } = actionLevel ?? level.DefaultActionLevel();

    public LogItem(
        ILifecycleService source,
        string message,
        Exception? exception,
        LogLevel level,
        ActionLevel? actionLevel) : this($" [{source.Name}|{source.Identifier}] {message}", exception, level, actionLevel)
    {}
    
    public LogItem(
        string module,
        string message,
        Exception? exception,
        LogLevel level,
        ActionLevel? actionLevel) : this($" [{module}] {message}", exception, level, actionLevel)
    {}

    public override string ToString()
    {
        return Exception == null ? $"[{Time:HH:mm:ss.fff}] {Message}" : $"[{Time:HH:mm:ss.fff}] ({Message}) {Exception.GetType().FullName}: {Exception.Message}";
    }

    public string ComposeMessage()
    {
        var result = $"[{Time:HH:mm:ss.fff}] [{Level.RealLevel().PrintName()}] [{ThreadName}]{Message}";
        if (Exception != null) result += $"\n{Exception}";
        return result;
    }
}
