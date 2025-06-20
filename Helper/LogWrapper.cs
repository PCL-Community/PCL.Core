using System;

namespace PCL.Core.Helper;

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}

public delegate void LogHandler(LogLevel level, string msg, bool isError = false, string? module = null, Exception? ex = null);

public enum InfoLevel
{
    Trace = LogLevel.Trace,
    Debug = LogLevel.Debug,
    Hint = LogLevel.Info,
    MsgBox = LogLevel.Warning
}

public enum ErrorLevel
{
    Debug = LogLevel.Debug,
    Hint = LogLevel.Info,
    MsgBox = LogLevel.Warning,
    Feedback = LogLevel.Error,
    Fatal = LogLevel.Fatal,
}

public static class LogWrapper
{
    public static event LogHandler? OnLog;
    
    public static void Info(string? module, string msg, InfoLevel level = InfoLevel.Hint)
    {
        OnLog?.Invoke((LogLevel)level, msg, false, module);
    }
    
    public static void Info(string msg, InfoLevel level = InfoLevel.Hint)
    {
        Info(null, msg, level);
    }
    
    public static void Error(Exception? ex, string? module, string msg, ErrorLevel level = ErrorLevel.Debug)
    {
        OnLog?.Invoke((LogLevel)level, msg, true, module, ex);
    }
    
    public static void Error(Exception? ex, string msg, ErrorLevel level = ErrorLevel.Debug)
    {
        Error(ex, null, msg, level);
    }

    public static void Error(string? module, string msg, ErrorLevel level = ErrorLevel.Debug)
    {
        Error(null, module, msg, level);
    }

    public static void Error(string msg, ErrorLevel level = ErrorLevel.Debug)
    {
        Error((string?)null, msg, level);
    }
    
    public static void Debug(string? module, string msg)
    {
        Info(module, msg, InfoLevel.Debug);
    }
    
    public static void Debug(string msg)
    {
        Debug(null, msg);
    }
    
    public static void Trace(string? module, string msg)
    {
        Info(module, msg, InfoLevel.Trace);
    }
    
    public static void Trace(string msg)
    {
        Trace(null, msg);
    }
}
