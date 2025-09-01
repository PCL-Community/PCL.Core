namespace PCL.Core.App.Configuration;

public interface IEventScope
{
    /// <summary>
    /// 传入事件观察器以观察事件。
    /// </summary>
    public void Observe(ConfigObserver observer);

    /// <summary>
    /// 取消观察事件。
    /// </summary>
    public bool Unobserve(ConfigObserver observer);

    /// <summary>
    /// 触发配置项事件。
    /// </summary>
    /// <param name="trigger">触发事件</param>
    /// <param name="argument">上下文参数</param>
    /// <param name="newValue">用于向事件参数传递的新值</param>
    /// <param name="bypassOldValue">若为 <c>true</c> 则向事件参数的旧值传递 <c>null</c>，否则传递当前值</param>
    /// <param name="fillNewValue">若为 <c>true</c>，当新值为 <c>null</c> 时将传递当前值或默认值</param>
    /// <returns></returns>
    public ConfigEventArgs? TriggerEvent(
        ConfigEvent trigger,
        object? argument,
        object? newValue,
        bool bypassOldValue = false,
        bool fillNewValue = false
    );
}

public static class IEventScopeExtension
{
    /// <summary>
    /// 传入事件类型与处理委托以观察事件。
    /// </summary>
    public static ConfigObserver Observe(this IEventScope scope,
        ConfigEvent trigger, ConfigEventHandler handler, bool isPreview = false)
    {
        var observer = new ConfigObserver(trigger, handler, isPreview);
        scope.Observe(observer);
        return observer;
    }
}
