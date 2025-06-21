using System;

namespace PCL.Core.Lifecycle;

/// <summary>
/// 日志服务专用接口。整个生命周期只能有一个日志服务，若出现第二个将会报错。
/// </summary>
public interface ILifecycleLogService : ILifecycleService
{
    /// <summary>
    /// 记录日志的事件
    /// </summary>
    /// <param name="source">日志来源</param>
    /// <param name="message">日志内容</param>
    /// <param name="ex">相关异常</param>
    /// <param name="level">日志等级</param>
    /// <param name="actionLevel">行为等级</param>
    public void OnLog(
        ILifecycleService source,
        string message,
        Exception? ex = null,
        LifecycleLogLevel level = LifecycleLogLevel.Trace,
        LifecycleActionLevel? actionLevel = null
    );
}
