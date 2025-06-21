using System;

namespace PCL.Core.Lifecycle;

/// <summary>
/// 注册生命周期服务项，将由生命周期管理统一创建实例，然后在指定生命周期自动启动或加入等待手动启动列表。<br/>
/// 使用此注解的类型必须实现 <see cref="ILifecycleService"/> 接口，否则将被忽略。<br/><br/>
/// <b>代码生成注意事项</b>：此注解的自动注册功能由 MSBuild 自定义任务运行 PowerShell
/// 脚本生成代码实现，该脚本通过正则表达式匹配文本来确定注解属性，因此请尽可能遵循以下两种标准写法以确保注解被匹配到：<br/><br/>
/// 1. <c>[LifecycleService(LifecycleState.Xxx), Priority = num]</c><br/>
/// 2. <c>[LifecycleService(LifecycleState.Xxx)]</c><br/><br/>
/// 其中 <c>num</c> 可以是一个整数或 <c>int.MaxValue</c> <c>int.MinValue</c> 之一
/// </summary>
/// <param name="startState">详见 <see cref="StartState"/></param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LifecycleService(LifecycleState startState) : Attribute
{
    /// <summary>
    /// 指定该服务项应于何种生命周期状态启动。生命周期管理将在指定的状态按照 <see cref="Priority"/> 自动启动服务项。
    /// </summary>
    public LifecycleState StartState { get; } = startState;
    
    /// <summary>
    /// 启动优先级。同一个生命周期状态有多个服务项需要启动时，将会按优先级数值<b>降序</b>启动。
    /// </summary>
    public int Priority { get; set; } = 0;
}
