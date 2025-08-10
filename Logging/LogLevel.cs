using System.Collections.Generic;

namespace PCL.Core.Logging;

/// <summary>
/// 日志等级。将十进制值 <c>% 100</c> 并显式转换可得到该等级默认对应的 <see cref="ActionLevel"/>
/// </summary>
public enum LogLevel
{
    Trace = 000 + ActionLevel.DebugLog,
    Debug = 100 + ActionLevel.DebugLog,
    Info = 200 + ActionLevel.NormalLog,
    Warning = 300 + ActionLevel.HintRed,
    Error = 400 + ActionLevel.MsgBoxRed,
    Fatal = 500 + ActionLevel.MsgBoxExit,
}

public static class LogLevelExtensions
{
    private static readonly Dictionary<LogLevel, string> _LevelNameMap = new()
    {
        [LogLevel.Trace] = "TRA",
        [LogLevel.Debug] = "DBG",
        [LogLevel.Info] = "INFO",
        [LogLevel.Warning] = "WARN",
        [LogLevel.Error] = "ERR!",
        [LogLevel.Fatal] = "FTL!"
    };

    public static string PrintName(this LogLevel level) => _LevelNameMap[level];
}
