using System.Collections.Generic;
using PCL.Core.App;

namespace PCL.Core.Logging;

public static class LogController
{
    /// <summary>
    /// 待处理的日志列表
    /// </summary>
    private static readonly List<LogItem> _PendingLogs = [];
    
    /// <summary>
    /// 当前日志服务，若未设置则会将日志存入待处理列表，待设置后统一处理
    /// </summary>
    public static ILifecycleLogService? CurrentLogService
    {
        get;
        set
        {
            field = value;
            if (_PendingLogs.Count == 0) return;
            
            // 清空待处理日志
            _PendingLogs.ForEach(item => field?.OnLog(item));
            _PendingLogs.Clear();
        }
    }
    
    public static void PushLog(LogItem item)
    {
        if (CurrentLogService != null)
        {
            CurrentLogService.OnLog(item);
            return;
        }

        _PendingLogs.Add(item);
    }
}