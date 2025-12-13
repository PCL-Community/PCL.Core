using System;
using System.Collections.Generic;

namespace PCL.Core.App;

[LifecycleService(LifecycleState.BeforeLoading)]
public partial class ArgumentsService : GeneralService
{
    #region Lifecycle
    
    private static LifecycleContext? _context;

    private static LifecycleContext Context => _context!;
    
    public ArgumentsService() : base("args", "参数处理", false) { _context = ServiceContext; }

    public override void Start()
    {
        _Initialize();
        _HandleArguments();
        Context.DeclareStopped();
    }
    #endregion
    
    #region Arguments Handle
    
    private readonly Dictionary<string, Func<string[],HandleResult>> _handlers = [];
    
    private void _HandleArguments()
    {
        var args = Basics.CommandLineArguments;

        foreach (var (name, handler) in _handlers)
        {
            var result = handler(args);
            switch (result.ResultType)
            {
                case HandleResultType.NotHandled: break;
                case HandleResultType.Handled:
                    Context.Info($"参数已被处理器 {name} 处理");
                    return;
                case HandleResultType.HandledAndExit:
                    Context.Info($"参数已被处理器 {name} 处理，程序将退出");
                    Context.RequestExit(result.ExitCode);
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        Context.Debug("无匹配的命令行参数处理器");
    }
    #endregion
}

[AttributeUsage(AttributeTargets.Method)]
public class ArgumentHandlerAttribute(string identifier) : Attribute
{
    public string Identifier { get; init; } = identifier;
}

public enum HandleResultType
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

public record HandleResult(HandleResultType ResultType, int ExitCode = 0);