using System;

namespace PCL.Core.LifecycleManagement;

/// <summary>
/// 若要获取服务项自身的上下文实例，请使用 <see cref="Lifecycle.GetContext"/> 。
/// </summary>
public class LifecycleContext(
    ILifecycleService service,
    Action<LifecycleLogItem> onLog,
    Action<int> onRequestExit,
    Action<string?> onRequestRestart,
    Action onDeclareStopped)
{
    public void CustomLog(
        string message,
        Exception? ex = null,
        LifecycleLogLevel level = LifecycleLogLevel.Trace,
        LifecycleActionLevel? actionLevel = null
    ) => onLog(new LifecycleLogItem(service, message, ex, level, actionLevel));
    
    public void Trace(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Trace, actionLevel);
    public void Debug(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Debug, actionLevel);
    public void Info(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Info, actionLevel);
    public void Warn(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Warning, actionLevel);
    public void Error(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Error, actionLevel);
    public void Fatal(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Fatal, actionLevel);

    /// <summary>
    /// 请求退出程序。仅可在 <see cref="LifecycleState.BeforeLoading"/> 时使用。
    /// </summary>
    /// <param name="statusCode">程序返回的状态码</param>
    public void RequestExit(int statusCode = 0) => onRequestExit(statusCode);
    
    /// <summary>
    /// 请求在程序退出时重启。调用该方法后，程序将在正常退出流程中自动执行重启，通常与退出程序结合使用。
    /// </summary>
    /// <param name="arguments">重启进程时使用的命令行参数</param>
    public void RequestRestartOnExit(string? arguments = null) => onRequestRestart(arguments);
    
    /// <summary>
    /// 标记自身已经结束运行。调用该方法将会直接从正在运行列表中移除该服务项，后续的 <c>Stop</c> 等均不会触发。仅可在 <c>Start</c> 方法中使用。
    /// </summary>
    public void DeclareStopped() => onDeclareStopped();
}
