namespace PCL.Core.LifecycleManagement;

/// <summary>
/// 事件/意外行为等级。
/// </summary>
public enum LifecycleActionLevel
{
    DebugLog = 00,
    NormalLog = 10,
    Hint = 20,
    HintRed = 21,
    MsgBox = 30,
    MsgBoxRed = 31,
    MsgBoxExit = 32,
}
