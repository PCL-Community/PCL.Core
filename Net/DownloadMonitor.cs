using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 下载性能指标
/// </summary>
public class DownloadMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public long BytesPerSecond { get; set; }
    public int ActiveConnections { get; set; }
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// 下载健康状态
/// </summary>
public enum DownloadHealth
{
    Excellent,  // > 80% 预期速度
    Good,       // 50-80% 预期速度
    Fair,       // 20-50% 预期速度
    Poor,       // < 20% 预期速度
    Critical    // 连接失败或严重错误
}

/// <summary>
/// 下载诊断信息
/// </summary>
public class DownloadDiagnosis
{
    public DownloadHealth Health { get; set; }
    public string HealthDescription { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public double EfficiencyScore { get; set; } // 0-100
}

/// <summary>
/// 下载监控器
/// 提供实时监控、性能分析和诊断功能
/// </summary>
public class DownloadMonitor : IDisposable
{
    private const string LogModule = "DownloadMonitor";
    
    private readonly ConcurrentQueue<DownloadMetrics> _metricsHistory = new();
    private readonly ConcurrentDictionary<string, DownloadStatistics> _activeDownloads = new();
    private readonly Timer _monitoringTimer;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    
    /// <summary>
    /// 监控间隔（毫秒）
    /// </summary>
    public int MonitoringInterval { get; set; } = 1000;
    
    /// <summary>
    /// 性能指标历史记录保留数量
    /// </summary>
    public int MaxHistoryCount { get; set; } = 300; // 5分钟历史
    
    /// <summary>
    /// 启用详细监控
    /// </summary>
    public bool EnableDetailedMonitoring { get; set; } = true;
    
    /// <summary>
    /// 性能警告阈值（字节/秒）
    /// </summary>
    public long PerformanceWarningThreshold { get; set; } = 100 * 1024; // 100KB/s
    
    /// <summary>
    /// 性能指标历史
    /// </summary>
    public IReadOnlyCollection<DownloadMetrics> MetricsHistory => _metricsHistory.ToArray();
    
    /// <summary>
    /// 当前活动下载数量
    /// </summary>
    public int ActiveDownloadCount => _activeDownloads.Count;
    
    /// <summary>
    /// 性能警告事件
    /// </summary>
    public event Action<DownloadDiagnosis>? PerformanceWarning;
    
    /// <summary>
    /// 下载完成事件
    /// </summary>
    public event Action<string, DownloadStatistics>? DownloadCompleted;
    
    public DownloadMonitor()
    {
        _monitoringTimer = new Timer(MonitorPerformance, null, 
            TimeSpan.FromMilliseconds(MonitoringInterval), 
            TimeSpan.FromMilliseconds(MonitoringInterval));
            
        LogWrapper.Info(LogModule, "下载监控器已启动");
    }
    
    /// <summary>
    /// 注册下载任务进行监控
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="task">下载任务</param>
    public void RegisterDownload(string taskId, EnhancedMultiThreadDownloadTask task)
    {
        if (_disposed) return;
        
        var stats = task.GetDetailedStatus();
        _activeDownloads[taskId] = stats;
        
        // 订阅任务状态变化
        task.StateChanged += (sender, oldState, newState) =>
        {
            if (newState is TaskState.Completed or TaskState.Failed or TaskState.Canceled)
            {
                OnDownloadCompleted(taskId, task);
            }
        };
        
        LogWrapper.Info(LogModule, $"注册下载任务: {taskId}");
    }
    
    /// <summary>
    /// 注销下载任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    public void UnregisterDownload(string taskId)
    {
        if (_activeDownloads.TryRemove(taskId, out var stats))
        {
            LogWrapper.Info(LogModule, $"注销下载任务: {taskId}");
        }
    }
    
    /// <summary>
    /// 获取总体统计信息
    /// </summary>
    /// <returns>总体统计</returns>
    public (long TotalBytes, long DownloadedBytes, double AverageSpeed, int ActiveTasks) GetOverallStatistics()
    {
        lock (_lockObject)
        {
            var stats = _activeDownloads.Values.ToArray();
            var totalBytes = stats.Sum(s => s.TotalBytes);
            var downloadedBytes = stats.Sum(s => s.DownloadedBytes);
            var averageSpeed = stats.Where(s => s.CurrentSpeed > 0).DefaultIfEmpty().Average(s => s.CurrentSpeed);
            
            return (totalBytes, downloadedBytes, averageSpeed, stats.Length);
        }
    }
    
    /// <summary>
    /// 获取性能分析报告
    /// </summary>
    /// <returns>性能报告</returns>
    public PerformanceReport GetPerformanceReport()
    {
        lock (_lockObject)
        {
            var metrics = _metricsHistory.ToArray();
            var stats = _activeDownloads.Values.ToArray();
            
            return new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                ActiveDownloads = stats.Length,
                TotalBytesTransferred = stats.Sum(s => s.DownloadedBytes),
                AverageSpeed = metrics.Where(m => m.BytesPerSecond > 0).DefaultIfEmpty().Average(m => m.BytesPerSecond),
                PeakSpeed = metrics.DefaultIfEmpty().Max(m => m?.BytesPerSecond ?? 0),
                TotalRetries = stats.Sum(s => s.RetryCount),
                AverageConnections = metrics.Where(m => m.ActiveConnections > 0).DefaultIfEmpty().Average(m => m.ActiveConnections),
                OverallHealth = CalculateOverallHealth(stats),
                Recommendations = GenerateRecommendations(stats, metrics)
            };
        }
    }
    
    /// <summary>
    /// 诊断下载性能
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>诊断结果</returns>
    public DownloadDiagnosis DiagnoseDownload(string taskId)
    {
        if (!_activeDownloads.TryGetValue(taskId, out var stats))
        {
            return new DownloadDiagnosis
            {
                Health = DownloadHealth.Critical,
                HealthDescription = "下载任务未找到",
                EfficiencyScore = 0
            };
        }
        
        var diagnosis = new DownloadDiagnosis();
        var issues = new List<string>();
        var recommendations = new List<string>();
        
        // 速度分析
        var expectedSpeed = EstimateExpectedSpeed(stats.TotalBytes);
        var speedRatio = stats.CurrentSpeed / expectedSpeed;
        
        if (speedRatio >= 0.8)
        {
            diagnosis.Health = DownloadHealth.Excellent;
            diagnosis.HealthDescription = "下载速度优秀";
        }
        else if (speedRatio >= 0.5)
        {
            diagnosis.Health = DownloadHealth.Good;
            diagnosis.HealthDescription = "下载速度良好";
        }
        else if (speedRatio >= 0.2)
        {
            diagnosis.Health = DownloadHealth.Fair;
            diagnosis.HealthDescription = "下载速度一般";
            issues.Add("下载速度低于预期");
            recommendations.Add("考虑增加线程数或检查网络连接");
        }
        else
        {
            diagnosis.Health = DownloadHealth.Poor;
            diagnosis.HealthDescription = "下载速度较差";
            issues.Add("下载速度严重低于预期");
            recommendations.Add("检查网络连接、增加重试次数或更换下载源");
        }
        
        // 重试分析
        if (stats.RetryCount > 10)
        {
            issues.Add($"重试次数过多: {stats.RetryCount}");
            recommendations.Add("检查网络稳定性或服务器可靠性");
        }
        
        // 效率分析
        var progressRate = stats.ProgressPercentage / stats.ElapsedTime.TotalMinutes;
        diagnosis.EfficiencyScore = Math.Min(100, speedRatio * 100);
        
        // 连接分析
        if (stats.ActiveThreads == 1 && stats.TotalBytes > 10 * 1024 * 1024) // 大于10MB
        {
            issues.Add("大文件使用单线程下载");
            recommendations.Add("检查服务器是否支持分块下载");
        }
        
        diagnosis.Issues = issues;
        diagnosis.Recommendations = recommendations;
        
        return diagnosis;
    }
    
    /// <summary>
    /// 获取实时性能指标
    /// </summary>
    /// <returns>当前性能指标</returns>
    public DownloadMetrics GetCurrentMetrics()
    {
        lock (_lockObject)
        {
            var stats = _activeDownloads.Values.ToArray();
            
            return new DownloadMetrics
            {
                Timestamp = DateTime.Now,
                BytesPerSecond = (long)stats.Sum(s => s.CurrentSpeed),
                ActiveConnections = stats.Sum(s => s.ActiveThreads),
                RetryCount = stats.Sum(s => s.RetryCount),
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageBytes = GetMemoryUsage()
            };
        }
    }
    
    /// <summary>
    /// 监控性能（定时器回调）
    /// </summary>
    private void MonitorPerformance(object? state)
    {
        if (_disposed) return;
        
        try
        {
            var currentMetrics = GetCurrentMetrics();
            
            // 添加到历史记录
            _metricsHistory.Enqueue(currentMetrics);
            
            // 限制历史记录数量
            while (_metricsHistory.Count > MaxHistoryCount)
            {
                _metricsHistory.TryDequeue(out _);
            }
            
            // 检查性能警告
            CheckPerformanceWarnings(currentMetrics);
            
            // 更新活动下载统计
            UpdateActiveDownloadStats();
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, "性能监控出错");
        }
    }
    
    /// <summary>
    /// 更新活动下载统计信息
    /// </summary>
    private void UpdateActiveDownloadStats()
    {
        // 这里可以从实际的下载任务中更新统计信息
        // 暂时保持空实现，实际使用时需要与具体的下载任务集成
    }
    
    /// <summary>
    /// 检查性能警告
    /// </summary>
    private void CheckPerformanceWarnings(DownloadMetrics metrics)
    {
        if (!EnableDetailedMonitoring) return;
        
        var issues = new List<string>();
        var recommendations = new List<string>();
        var health = DownloadHealth.Good;
        
        // 检查下载速度
        if (metrics.BytesPerSecond > 0 && metrics.BytesPerSecond < PerformanceWarningThreshold)
        {
            health = DownloadHealth.Poor;
            issues.Add($"下载速度过低: {metrics.BytesPerSecond / 1024:F1} KB/s");
            recommendations.Add("检查网络连接或增加并发数");
        }
        
        // 检查CPU使用率
        if (metrics.CpuUsagePercent > 80)
        {
            issues.Add($"CPU使用率过高: {metrics.CpuUsagePercent:F1}%");
            recommendations.Add("考虑减少并发线程数");
        }
        
        // 检查内存使用
        if (metrics.MemoryUsageBytes > 500 * 1024 * 1024) // 500MB
        {
            issues.Add($"内存使用过高: {metrics.MemoryUsageBytes / 1024 / 1024:F1} MB");
            recommendations.Add("检查是否有内存泄漏或减少缓冲区大小");
        }
        
        if (issues.Any())
        {
            var diagnosis = new DownloadDiagnosis
            {
                Health = health,
                HealthDescription = "检测到性能问题",
                Issues = issues,
                Recommendations = recommendations,
                EfficiencyScore = CalculateEfficiencyScore(metrics)
            };
            
            PerformanceWarning?.Invoke(diagnosis);
        }
    }
    
    /// <summary>
    /// 计算效率分数
    /// </summary>
    private double CalculateEfficiencyScore(DownloadMetrics metrics)
    {
        var score = 100.0;
        
        // 速度评分
        if (metrics.BytesPerSecond < PerformanceWarningThreshold)
            score -= 30;
        
        // CPU评分
        if (metrics.CpuUsagePercent > 80)
            score -= 20;
        else if (metrics.CpuUsagePercent > 60)
            score -= 10;
        
        // 内存评分
        if (metrics.MemoryUsageBytes > 500 * 1024 * 1024)
            score -= 20;
        else if (metrics.MemoryUsageBytes > 200 * 1024 * 1024)
            score -= 10;
        
        return Math.Max(0, score);
    }
    
    /// <summary>
    /// 下载完成处理
    /// </summary>
    private void OnDownloadCompleted(string taskId, EnhancedMultiThreadDownloadTask task)
    {
        if (_activeDownloads.TryRemove(taskId, out var stats))
        {
            var finalStats = task.GetDetailedStatus();
            DownloadCompleted?.Invoke(taskId, finalStats);
            
            LogWrapper.Info(LogModule, $"下载完成: {taskId}, " +
                $"平均速度: {finalStats.AverageSpeed / 1024 / 1024:F2} MB/s, " +
                $"重试次数: {finalStats.RetryCount}");
        }
    }
    
    /// <summary>
    /// 估算预期下载速度
    /// </summary>
    private double EstimateExpectedSpeed(long fileSize)
    {
        // 基于文件大小的简单估算，实际应用中可以基于历史数据
        if (fileSize < 1024 * 1024) // < 1MB
            return 500 * 1024; // 500KB/s
        else if (fileSize < 100 * 1024 * 1024) // < 100MB
            return 2 * 1024 * 1024; // 2MB/s
        else
            return 5 * 1024 * 1024; // 5MB/s
    }
    
    /// <summary>
    /// 计算整体健康状态
    /// </summary>
    private DownloadHealth CalculateOverallHealth(DownloadStatistics[] stats)
    {
        if (!stats.Any()) return DownloadHealth.Good;
        
        var avgSpeed = stats.Average(s => s.CurrentSpeed);
        var totalRetries = stats.Sum(s => s.RetryCount);
        var avgProgress = stats.Average(s => s.ProgressPercentage);
        
        if (avgSpeed > 2 * 1024 * 1024 && totalRetries < 10) // > 2MB/s, < 10 retries
            return DownloadHealth.Excellent;
        else if (avgSpeed > 1024 * 1024 && totalRetries < 20) // > 1MB/s, < 20 retries
            return DownloadHealth.Good;
        else if (avgSpeed > 512 * 1024) // > 512KB/s
            return DownloadHealth.Fair;
        else
            return DownloadHealth.Poor;
    }
    
    /// <summary>
    /// 生成性能建议
    /// </summary>
    private List<string> GenerateRecommendations(DownloadStatistics[] stats, DownloadMetrics[] metrics)
    {
        var recommendations = new List<string>();
        
        if (!stats.Any()) return recommendations;
        
        var avgSpeed = stats.Average(s => s.CurrentSpeed);
        var totalRetries = stats.Sum(s => s.RetryCount);
        var avgThreads = stats.Average(s => s.ActiveThreads);
        
        if (avgSpeed < 1024 * 1024) // < 1MB/s
        {
            recommendations.Add("考虑增加下载线程数以提高速度");
        }
        
        if (totalRetries > 50)
        {
            recommendations.Add("重试次数较多，建议检查网络稳定性");
        }
        
        if (avgThreads < 2 && stats.Any(s => s.TotalBytes > 10 * 1024 * 1024))
        {
            recommendations.Add("大文件建议使用多线程下载");
        }
        
        if (metrics.Any(m => m.CpuUsagePercent > 70))
        {
            recommendations.Add("CPU使用率较高，可适当减少并发数");
        }
        
        return recommendations;
    }
    
    /// <summary>
    /// 获取CPU使用率（简化实现）
    /// </summary>
    private double GetCpuUsage()
    {
        // 实际实现应该使用PerformanceCounter或其他系统API
        return 0.0;
    }
    
    /// <summary>
    /// 获取内存使用量（简化实现）
    /// </summary>
    private long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        _monitoringTimer?.Dispose();
        _activeDownloads.Clear();
        _disposed = true;
        
        LogWrapper.Info(LogModule, "下载监控器已释放");
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 性能报告
/// </summary>
public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public int ActiveDownloads { get; set; }
    public long TotalBytesTransferred { get; set; }
    public double AverageSpeed { get; set; }
    public long PeakSpeed { get; set; }
    public int TotalRetries { get; set; }
    public double AverageConnections { get; set; }
    public DownloadHealth OverallHealth { get; set; }
    public List<string> Recommendations { get; set; } = new();
    
    public override string ToString()
    {
        return $"性能报告 ({GeneratedAt:yyyy-MM-dd HH:mm:ss})\n" +
               $"活动下载: {ActiveDownloads}\n" +
               $"总传输: {TotalBytesTransferred / 1024 / 1024:F1} MB\n" +
               $"平均速度: {AverageSpeed / 1024 / 1024:F2} MB/s\n" +
               $"峰值速度: {PeakSpeed / 1024 / 1024:F2} MB/s\n" +
               $"总重试: {TotalRetries}\n" +
               $"整体健康: {OverallHealth}";
    }
}
