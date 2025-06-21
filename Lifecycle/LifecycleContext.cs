using System;

namespace PCL.Core.Lifecycle;

/// <summary>
/// 若要获取服务项自身的上下文实例，请使用 <see cref="Lifecycle.GetContext"/> 。
/// </summary>
public class LifecycleContext(ILifecycleService service, Action<LifecycleLogItem> logAction)
{
    public virtual void CustomLog(
        string message,
        Exception? ex = null,
        LifecycleLogLevel level = LifecycleLogLevel.Trace,
        LifecycleActionLevel? actionLevel = null
    ) => logAction(new LifecycleLogItem(service, message, ex, level, actionLevel));
    
    public void Trace(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Trace, actionLevel);
    public void Debug(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Debug, actionLevel);
    public void Info(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Info, actionLevel);
    public void Warn(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Warning, actionLevel);
    public void Error(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Error, actionLevel);
    public void Fatal(string message, Exception? ex = null, LifecycleActionLevel? actionLevel = null) => CustomLog(message, ex, LifecycleLogLevel.Fatal, actionLevel);

    /// <summary>
    /// 请求退出程序。仅可在 <see cref="LifecycleState.BeforeLoading"/> 时使用。
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void RequestExit()
    {
        throw new NotImplementedException();
    }

    // -- EMPTY INSTANCE --
    
    private class EmptyLifecycleService : ILifecycleService
    {
        public string Name => "未定义";
        public string Identifier => "undefined";
        public bool SupportAsyncStart => false;
        public void Start() { }
        public void Stop() { }
    }

    public static readonly LifecycleContext Empty = Lifecycle.GetContext(new EmptyLifecycleService());
}
