using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Utils.Threading;

namespace PCL.Core.Net;

/// <summary>
/// 优化下载器
/// </summary>
public class OptimizedDownloader : IDisposable
{
    private const string LogModule = "UltraDownload";
    
    // 全局下载器计数器 - 使用Interlocked保证线程安全
    private static int _globalSchedulerCount;
    
    // 高性能Channel替代ConcurrentQueue - 更好的背压控制
    private readonly Channel<DownloadItem> _downloadChannel;
    private readonly ChannelWriter<DownloadItem> _downloadWriter;
    private readonly ChannelReader<DownloadItem> _downloadReader;
    
    // 无锁的并发控制
    private volatile int _currentParallelCount;
    private readonly int _maxParallels;
    private readonly int _refreshIntervalMs;
    private readonly TimeSpan _timeout;
    
    // 高性能任务调度器 - 避免Task.Run的开销
    private readonly LimitedConcurrencyLevelTaskScheduler _taskScheduler;
    private readonly TaskFactory _taskFactory;
    
    // 智能自适应参数
    private double _averageDownloadSpeed; // bytes/ms
    private volatile int _optimalChunkSize = 1024 * 1024; // 1MB初始值
    
    // 取消令牌和生命周期管理
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _schedulerTask;
    
    /// <summary>
    /// 调度器ID
    /// </summary>
    public int SchedulerId { get; }
    
    /// <summary>
    /// 当前并发任务数
    /// </summary>
    public int CurrentParallelCount => _currentParallelCount;
    
    /// <summary>
    /// 正在运行的下载项 - 使用高性能的只读集合
    /// </summary>
    public IReadOnlyList<DownloadItem> RunningItems { get; private set; } = Array.Empty<DownloadItem>();
    
    /// <summary>
    /// 全局下载器实例数量
    /// </summary>
    public static int GlobalSchedulerCount => _globalSchedulerCount;
    
    /// <summary>
    /// 全局并发限制 - 动态调整
    /// </summary>
    public static volatile int GlobalParallelLimit = Environment.ProcessorCount * 4;
    
    /// <summary>
    /// 智能带宽检测 - 根据网络状况自动调整参数
    /// </summary>
    public static volatile bool EnableAdaptiveBandwidth = true;
    
    public OptimizedDownloader(
        int maxParallels = int.MaxValue,
        int refreshIntervalMs = 100, // 更频繁的刷新以提高响应性
        TimeSpan? timeout = null)
    {
        SchedulerId = Interlocked.Increment(ref _globalSchedulerCount);
        _maxParallels = Math.Min(maxParallels, GlobalParallelLimit);
        _refreshIntervalMs = refreshIntervalMs;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        
        // 创建高性能Channel - 使用有界Channel以防内存爆炸
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true, // 只有一个调度器线程读取
            SingleWriter = false // 多个线程可能添加任务
        };
        _downloadChannel = Channel.CreateBounded<DownloadItem>(channelOptions);
        _downloadWriter = _downloadChannel.Writer;
        _downloadReader = _downloadChannel.Reader;
        
        // 创建专用任务调度器 - 避免线程池饥饿
        _taskScheduler = new LimitedConcurrencyLevelTaskScheduler(_maxParallels * 2);
        _taskFactory = new TaskFactory(_taskScheduler);
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // 启动超高性能调度器任务
        _schedulerTask = Task.Run(UltraFastSchedulerLoop, _cancellationTokenSource.Token);
        
        LogWrapper.Info(LogModule, $"#{SchedulerId} 超级下载器已启动 (最大并发: {_maxParallels})");
    }
    
    /// <summary>
    /// 添加下载项 - 使用Channel的高性能异步方法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddItem(DownloadItem item)
    {
        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return false;
            
        LogWrapper.Trace(LogModule, $"#{SchedulerId} 新增超级下载项: {item}");
        return _downloadWriter.TryWrite(item);
    }
    
    /// <summary>
    /// 异步添加下载项
    /// </summary>
    public async ValueTask<bool> AddItemAsync(DownloadItem item, CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return false;
            
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(
            _cancellationTokenSource.Token, cancellationToken);
            
        try
        {
            await _downloadWriter.WriteAsync(item, combined.Token);
            LogWrapper.Trace(LogModule, $"#{SchedulerId} 异步新增超级下载项: {item}");
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// 调度器主循环，无锁设计
    /// </summary>
    private async Task UltraFastSchedulerLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        var runningItems = new List<DownloadItem>(256); // 预分配容量
        var completedItems = new List<DownloadItem>(32);
        
        try
        {
            await foreach (var item in _downloadReader.ReadAllAsync(_cancellationTokenSource.Token))
            {
                // 清理已完成的任务
                CleanupCompletedTasks(runningItems, completedItems);
                
                // 检查是否可以启动新任务
                if (CanStartNewTask())
                {
                    await StartUltraFastDownload(item);
                    runningItems.Add(item);
                }
                else
                {
                    // 暂时无法处理，重新排队
                    if (_downloadWriter.TryWrite(item))
                    {
                        // 短暂等待避免忙等待
                        await Task.Delay(_refreshIntervalMs / 4, _cancellationTokenSource.Token);
                    }
                }
                
                // 定期更新运行状态
                if (stopwatch.ElapsedMilliseconds > _refreshIntervalMs)
                {
                    UpdateRunningItemsSnapshot(runningItems);
                    UpdateAdaptiveParameters(runningItems);
                    stopwatch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogWrapper.Info(LogModule, $"#{SchedulerId} 调度器已取消");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, LogModule, $"#{SchedulerId} 调度器发生错误");
        }
    }
    
    /// <summary>
    /// 检查是否可以启动新任务，无锁检查
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanStartNewTask()
    {
        var current = _currentParallelCount;
        return current < _maxParallels && current < GlobalParallelLimit;
    }
    
    /// <summary>
    /// 启动下载，避免Task.Run开销
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task StartUltraFastDownload(DownloadItem item)
    {
        Interlocked.Increment(ref _currentParallelCount);
        
        try
        {
            // 使用专用TaskFactory避免线程池竞争
            var downloadTask = _taskFactory.StartNew(async () =>
            {
                try
                {
                    await ExecuteUltraFastDownload(item);
                }
                catch (Exception ex)
                {
                    LogWrapper.Error(ex, LogModule, $"下载任务执行出错: {item}");
                    item.Cancel(true);
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.None, _taskScheduler);
            
            // 等待任务开始，但不等待完成
            await downloadTask;
        }
        finally
        {
            // 确保计数器正确递减
            Interlocked.Decrement(ref _currentParallelCount);
        }
    }
    
    /// <summary>
    /// 执行下载，智能分片算法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task ExecuteUltraFastDownload(DownloadItem item)
    {
        if (item.Status == DownloadItemStatus.Waiting)
        {
            // 智能初始分片策略
            var optimalSegmentCount = CalculateOptimalSegmentCount(item);
            await StartInitialSegments(item, optimalSegmentCount);
        }
        else
        {
            // 动态分片调整
            await OptimizeExistingSegments(item);
        }
    }
    
    /// <summary>
    /// 计算最优分片数量 - 基于网络状况和文件大小
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CalculateOptimalSegmentCount(DownloadItem item)
    {
        if (item.ContentLength <= 0) return 1; // 未知大小，单线程
        
        // 基于文件大小的自适应分片
        var fileSizeMB = item.ContentLength / (1024 * 1024);
        var baseSegments = fileSizeMB switch
        {
            < 1 => 1,      // 小于1MB，单线程
            < 10 => 2,     // 1-10MB，2线程
            < 100 => 4,    // 10-100MB，4线程
            < 1000 => 8,   // 100MB-1GB，8线程
            _ => 16        // 超过1GB，16线程
        };
        
        // 根据网络速度调整
        if (_averageDownloadSpeed > 10.0) // 高速网络
            baseSegments = Math.Min(baseSegments * 2, _maxParallels);
        
        return Math.Min(baseSegments, GlobalParallelLimit);
    }
    
    /// <summary>
    /// 启动初始分片 - 并行启动多个分片
    /// </summary>
    private async Task StartInitialSegments(DownloadItem item, int segmentCount)
    {
        if (segmentCount <= 1)
        {
            // 单线程下载
            await item.NewSegment(0, null, CreateErrorCallback(item));
            return;
        }
        
        // 多线程分片下载
        var segmentSize = item.ContentLength / segmentCount;
        var tasks = new Task[segmentCount];
        
        for (int i = 0; i < segmentCount; i++)
        {
            var start = i * segmentSize;
            var end = (i == segmentCount - 1) ? (long?)null : (i + 1) * segmentSize - 1;
            
            tasks[i] = item.NewSegment(start, end, CreateErrorCallback(item));
        }
        
        // 并行等待所有分片启动
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// 优化现有分片 - 动态分片调整算法
    /// </summary>
    private async Task OptimizeExistingSegments(DownloadItem item)
    {
        var currentTime = DateTime.Now;
        
        for (var node = item.Segments.First; node != null; node = node.Next)
        {
            var segment = node.Value;
            
            if (segment.Status != DownloadSegmentStatus.Running) continue;
            
            // 检查超时
            if (currentTime - segment.CurrentChunkStartTime > _timeout)
            {
                await item.RestartSegment(node, true);
                break;
            }
            
            // 智能分片分裂 - 基于下载速度和剩余大小
            if (ShouldSplitSegment(segment))
            {
                var splitPoint = CalculateOptimalSplitPoint(segment);
                await item.NewSegment(splitPoint, segment.EndPosition, CreateErrorCallback(item), node);
                break;
            }
        }
    }
    
    /// <summary>
    /// 判断是否应该分裂分片 - 智能算法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldSplitSegment(DownloadSegment segment)
    {
        // 剩余数据太少，不值得分裂
        if (segment.RemainingLength < _optimalChunkSize * 2) return false;
        
        // 下载速度太慢，需要更多线程
        var currentSpeed = segment.ChunkSize / Math.Max(segment.LastChunkElapsedTime.TotalMilliseconds, 1.0);
        return currentSpeed < _averageDownloadSpeed * 0.5; // 低于平均速度50%
    }
    
    /// <summary>
    /// 计算最优分裂点
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long CalculateOptimalSplitPoint(DownloadSegment segment)
    {
        // 基于当前进度和剩余大小的智能分裂
        var segmentLength = segment.EndPosition - segment.StartPosition + 1;
        var progress = (double)(segment.NextPosition - segment.StartPosition) / segmentLength;
        
        if (progress < 0.3) // 进度较少，从中间分裂
            return (segment.NextPosition + segment.EndPosition) / 2;
        else // 进度较多，从3/4处分裂
            return segment.NextPosition + (segment.EndPosition - segment.NextPosition) * 3 / 4;
    }
    
    /// <summary>
    /// 创建错误回调 - 优化的错误处理
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Action<DownloadSegmentStatus, Exception?> CreateErrorCallback(DownloadItem item)
    {
        return (status, exception) =>
        {
            if (status == DownloadSegmentStatus.FailedNotSupportRange)
            {
                item.TrySegment = false;
                return;
            }
            
            LogWrapper.Warn(exception, LogModule, $"下载失败 ({(int)status}): {item}");
            item.Cancel(true);
        };
    }
    
    /// <summary>
    /// 清理已完成的任务 - 高效的列表操作
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void CleanupCompletedTasks(List<DownloadItem> runningItems, List<DownloadItem> completedItems)
    {
        completedItems.Clear();
        
        // 收集已完成的项目
        for (int i = 0; i < runningItems.Count; i++)
        {
            var item = runningItems[i];
            if (item.Status is DownloadItemStatus.Cancelled or DownloadItemStatus.Success)
            {
                completedItems.Add(item);
            }
        }
        
        // 批量移除已完成的项目
        foreach (var completed in completedItems)
        {
            runningItems.Remove(completed);
        }
    }
    
    /// <summary>
    /// 更新运行项目快照 - 无锁更新
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRunningItemsSnapshot(List<DownloadItem> runningItems)
    {
        // 创建只读快照，避免锁
        RunningItems = runningItems.ToArray();
    }
    
    /// <summary>
    /// 更新自适应参数 - 机器学习式优化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void UpdateAdaptiveParameters(List<DownloadItem> runningItems)
    {
        if (!EnableAdaptiveBandwidth || runningItems.Count == 0) return;
        
        double totalSpeed = 0;
        int speedSamples = 0;
        
        foreach (var item in runningItems)
        {
            for (var node = item.Segments.First; node != null; node = node.Next)
            {
                var segment = node.Value;
                if (segment.Status == DownloadSegmentStatus.Running && segment.LastChunkElapsedTime.TotalMilliseconds > 0)
                {
                    var speed = segment.ChunkSize / segment.LastChunkElapsedTime.TotalMilliseconds;
                    totalSpeed += speed;
                    speedSamples++;
                }
            }
        }
        
        if (speedSamples > 0)
        {
            var newAverageSpeed = totalSpeed / speedSamples;
            // 使用指数移动平均避免剧烈波动
            _averageDownloadSpeed = _averageDownloadSpeed * 0.8 + newAverageSpeed * 0.2;
            
            // 动态调整块大小
            _optimalChunkSize = _averageDownloadSpeed > 1.0 
                ? Math.Max(512 * 1024, Math.Min(8 * 1024 * 1024, (int)(_averageDownloadSpeed * 1000)))
                : 512 * 1024;
        }
    }
    
    /// <summary>
    /// 完成所有下载并优雅关闭
    /// </summary>
    public async Task CompleteAsync()
    {
        _downloadWriter.Complete();
        await _schedulerTask;
        LogWrapper.Info(LogModule, $"#{SchedulerId} 超级下载器已完成所有任务");
    }
    
    /// <summary>
    /// 取消所有下载
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
        _downloadWriter.Complete();
        LogWrapper.Info(LogModule, $"#{SchedulerId} 超级下载器已取消");
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            Cancel();
        }
        
        _cancellationTokenSource?.Dispose();
        // 注意：LimitedConcurrencyLevelTaskScheduler 可能没有 Dispose 方法
        
        Interlocked.Decrement(ref _globalSchedulerCount);
    }
}