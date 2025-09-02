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
