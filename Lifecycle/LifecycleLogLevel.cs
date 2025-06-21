namespace PCL.Core.Lifecycle;

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
