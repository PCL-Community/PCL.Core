using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PCL.Core.App;

namespace PCL.Core.Logging;

public static class LogController
{

    /// <summary>
    /// 待处理的日志存放目录
    /// </summary>
    public static string PendingLogDirectory { get; set; } = @"PCL\Log";
    
    /// <summary>
    /// 待处理的日志文件名
    /// </summary>
    public static string PendingLogFileName { get; set; } = "LastPending.log";
    
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
            
            // 清空待处理日志
            lock (_PendingLogs)
            {
                if (_PendingLogs.Count == 0) return;
                
                _PendingLogs.ForEach(item => field?.OnLog(item));
                _PendingLogs.Clear();
            }
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
    
    public static void SavePendingLogs()
    {
        if (_PendingLogs.Count == 0)
        {
            Console.WriteLine("[Log] No pending logs");
            return;
        }
        try
        {
            // 直接写入剩余未输出日志到程序目录
            var path = Path.Combine(PendingLogDirectory, PendingLogFileName);
            if (!Path.IsPathRooted(path)) path = Path.Combine(Basics.ExecutableDirectory, path);
            Directory.CreateDirectory(Basics.GetParentPathOrDefault(path));
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, Encoding.UTF8);
            foreach (var item in _PendingLogs) writer.WriteLine(item.ComposeMessage());
            Console.WriteLine($"[Log] Pending logs saved to {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("[Log] Error saving pending logs, writing to stdout...");
            foreach (var item in _PendingLogs) Console.WriteLine(item.ComposeMessage());
        }
    }
}