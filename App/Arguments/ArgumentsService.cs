using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.App.Arguments;

[LifecycleService(LifecycleState.BeforeLoading)]
public partial class ArgumentsService : GeneralService
{
    #region Lifecycle
    
    private static LifecycleContext? _context;

    private static LifecycleContext Context => _context!;
    
    public ArgumentsService() : base("args", "参数处理", false) { _context = ServiceContext; }

    public override void Start()
    {
        _InitializeHandlers();
        _HandleArguments();
        Context.DeclareStopped();
    }
    #endregion
    
    #region Arguments Handle
    
    private void _HandleArguments()
    {
        var args = Basics.CommandLineArguments;

        foreach (var handler in _handlers)
        {
            var result = handler.Handle(args);
            switch (result)
            {
                case HandleResult.NotHandled: break;
                case HandleResult.Handled:
                    Context.Info($"参数已被处理器 {handler.Identifier} 处理");
                    return;
                case HandleResult.HandledAndExit:
                    Context.Info($"参数已被处理器 {handler.Identifier} 处理，程序将退出");
                    Context.RequestExit();
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        Context.Debug("无匹配的命令行参数处理器");
    }
    #endregion
}