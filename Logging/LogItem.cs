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
    
    /// <summary>
    /// 使用生命周期服务作为日志来源创建日志项
    /// </summary>
    /// <param name="source">源服务</param>
    /// <param name="message">日志消息内容</param>
    /// <param name="exception">相关异常对象，若无则为 null</param>
    /// <param name="level">日志等级</param>
    /// <param name="actionLevel">该日志项对应的操作等级</param>
    public LogItem(
        ILifecycleService source,
        string message,
        Exception? exception,
        LogLevel level,
        ActionLevel? actionLevel) : this($" [{source.Name}|{source.Identifier}] {message}", exception, level, actionLevel)
    {}
    
    /// <summary>
    /// 使用模块名称作为日志来源创建日志项
    /// </summary>
    /// <param name="module">模块名称</param>
    /// <param name="message">日志消息内容</param>
    /// <param name="exception">相关异常对象，若无则为 null</param>
    /// <param name="level">日志等级</param>
    /// <param name="actionLevel">该日志项对应的操作等级</param>

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