using System;

namespace PCL.Core.Lifecycle;

/// <summary>
/// 生命周期服务项的信息记录
/// </summary>
[Serializable]
public record LifecycleServiceInfo
{
    private readonly ILifecycleService _service;
    public string Identifier => _service.Identifier;
    public string Name => _service.Name;
    public bool CanStartAsync => _service.SupportAsyncStart;
    public LifecycleState StartState { get; }
    
    /// <summary>
    /// 服务开始运行的时间。初始值为调用 <c>Start()</c> 方法的时刻，在 <c>Start()</c> 方法结束之后会更新一次。
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.Now;
    
    /// <summary>
    /// 本 record 应由生命周期管理自动构造，若无特殊情况，请勿手动调用。
    /// </summary>
    /// <param name="service">生命周期服务项实例</param>
    /// <param name="startState">启动的生命周期状态</param>
    public LifecycleServiceInfo(ILifecycleService service, LifecycleState startState)
    {
        service.Start();
        _service = service;
        StartState = startState;
        StartTime = DateTime.Now;
    }
}
