namespace PCL.Core.App.Configuration.NTraffic;

/// <summary>
/// 物流中心模型。
/// </summary>
public interface ITrafficCenter
{
    /// <summary>
    /// 进行物流操作时触发的事件。
    /// </summary>
    public event TrafficEventHandler? Traffic;

    /// <summary>
    /// 预览物流操作时触发的事件。
    /// </summary>
    public event PreviewTrafficEventHandler? PreviewTraffic;
}

public static class TrafficCenterExtension
{
    /// <summary>
    /// 尝试将 <see cref="IConfigProvider"/> 实例转换到物流中心。
    /// </summary>
    public static ITrafficCenter? TryGetTrafficCenter(this IConfigProvider configProvider)
        => configProvider as TrafficCenter;

    /// <summary>
    /// 将 <see cref="IConfigProvider"/> 实例转换到物流中心。
    /// </summary>
    /// <exception cref="System.InvalidCastException">该实例的实现不是物流中心</exception>
    public static ITrafficCenter GetTrafficCenter(this IConfigProvider configProvider)
        => (TrafficCenter)configProvider;
}
