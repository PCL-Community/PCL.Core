using System;

namespace PCL.Core.App.Arguments;

public interface IArgumentHandler
{
    public string Identifier { get; }
    
    protected LifecycleContext ParentContext { get; init; }
    
    public HandleResult Handle(string[] args);
}

public abstract class GeneralHandler(string identifier) : IArgumentHandler
{
    public string Identifier => identifier;

    public required LifecycleContext ParentContext { get; init; }

    public abstract HandleResult Handle(string[] args);
}

[AttributeUsage(AttributeTargets.Class)]
public class ArgumentHandlerAttribute : Attribute;

public enum HandleResult
{
    /// <summary>
    /// 参数未被处理
    /// </summary>
    NotHandled,
    /// <summary>
    /// 参数已被处理
    /// </summary>
    Handled,
    /// <summary>
    /// 参数已被处理，且请求程序退出
    /// </summary>
    HandledAndExit,
}