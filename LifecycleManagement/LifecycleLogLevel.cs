using System.Collections.Generic;

namespace PCL.Core.LifecycleManagement;

/// <summary>
/// 日志等级。将十进制值 <c>% 100</c> 并显式转换可得到该等级默认对应的 <see cref="LifecycleActionLevel"/>
/// </summary>
public enum LifecycleLogLevel
{
    Trace = 000 + LifecycleActionLevel.DebugLog,
    Debug = 100 + LifecycleActionLevel.DebugLog,
    Info = 200 + LifecycleActionLevel.NormalLog,
    Warning = 300 + LifecycleActionLevel.HintRed,
    Error = 400 + LifecycleActionLevel.MsgBoxRed,
    Fatal = 500 + LifecycleActionLevel.MsgBoxExit,
}

public static class LifecycleLogLevelExtensions
{
    private static readonly Dictionary<LifecycleLogLevel, string> LevelNameMap = new()
    {
        [LifecycleLogLevel.Trace] = "TRA",
        [LifecycleLogLevel.Debug] = "DBG",
        [LifecycleLogLevel.Info] = "INFO",
        [LifecycleLogLevel.Warning] = "WARN",
        [LifecycleLogLevel.Error] = "ERR!",
        [LifecycleLogLevel.Fatal] = "FTL!"
    };

    public static string PrintName(this LifecycleLogLevel level) => LevelNameMap[level];
}
