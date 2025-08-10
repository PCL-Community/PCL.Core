using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public class Downloader(
    int maxParallels = int.MaxValue,
    int? refreshInterval = null,
    TimeSpan? timeout = null)
{
    private const string LogModule = "Download";

    private static int _schedulerCount = 0;
    
    /// <summary>
    /// 已实例化的调度器计数。该计数只与实例化行为有关，相关调度线程启动或停止均不会影响该计数。
    /// </summary>
    public static int SchedulerCount => _schedulerCount;
    
    /// <summary>
    /// 调度器默认刷新间隔。<br/>
    /// 当没有为一个调度器指定 <see cref="RefreshInterval"/> 时，将使用此全局默认值。
    /// </summary>
    public static int DefaultRefreshInterval { get; set; } = 500;
    
    /// <summary>
    /// 任务分片默认超时。若某个分片超时未传递任何数据将会重启该分片，并减少一次重试次数。<br/>
    /// 当未指定 <see cref="Timeout"/> 时，将使用此全局默认值。
    /// </summary>
    public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// 限制全局最大并发数量。该数量由所有实例共享，更改后将于下一个任务分块生效。
    /// </summary>
    public static int ParallelTaskLimit { get; set; } = 16;

    private int? _refreshInterval = refreshInterval;

    /// <summary>
    /// 调度器刷新间隔。该间隔仅对该实例生效，未指定值时，将使用全局默认值 <see cref="DefaultRefreshInterval"/>。<br/>
    /// 使用 <see cref="ClearRefreshInterval"/> 可使此值变为未指定状态。
    /// </summary>
    public int RefreshInterval
    {
        get => _refreshInterval ?? DefaultRefreshInterval;
        set => _refreshInterval = value;
    }

    /// <summary>
    /// 清除调度器刷新间隔。清除后将回到未指定状态，并使用全局默认值。
    /// </summary>
    /// <returns>若原值为未指定状态则返回 <c>false</c>，否则返回 <c>true</c></returns>
    public bool ClearRefreshInterval()
    {
        if (_refreshInterval == null) return false;
        _refreshInterval = null;
        return true;
    }

    private TimeSpan? _timeout = timeout;

    /// <summary>
    /// 任务分片超时。该超时仅对该实例生效，未指定值时，将使用全局默认值 <see cref="DefaultTimeout"/>。<br/>
    /// 使用 <see cref="ClearTimeout"/> 可使此值变为未指定状态。
    /// </summary>
    public TimeSpan Timeout
    {
        get => _timeout ?? DefaultTimeout;
        set => _timeout = value;
    }

    /// <summary>
    /// 清除任务分片超时。清除后将回到未指定状态，并使用全局默认值。
    /// </summary>
    /// <returns>若原值为未指定状态则返回 <c>false</c>，否则返回 <c>true</c></returns>
    public bool ClearTimeout()
    {
        if (_timeout == null) return false;
        _timeout = null;
        return true;
    }

    /// <summary>
    /// 限制最大并发数量。该数量仅作用于该实例，更改后将于下一个任务分块生效。
    /// </summary>
    public int MaxParallels { get; set; } = maxParallels;

    /// <summary>
    /// 当前实例的调度器 ID。
    /// </summary>
    public int SchedulerId { get; } = ++_schedulerCount;

    /// <summary>
    /// 正在进行的下载项。
    /// </summary>
    public DownloadItem[] RunningItems { get; private set; } = [];

    private readonly ConcurrentQueue<DownloadItem> _downloadQueue = [];

    private Thread? _schedulerThread;

    private readonly AutoResetEvent _taskEvent = new(false);

    /// <summary>
    /// 添加一个下载项。
    /// </summary>
    /// <param name="item">要添加的下载项</param>
    public void AddItem(DownloadItem item)
    {
        LogWrapper.Trace(LogModule, $"#{SchedulerId} 新增项目: {item}");
        _downloadQueue.Enqueue(item);
        _taskEvent.Set();
    }

    /// <summary>
    /// 手动激活自动暂停的调度器。<br/>
    /// 当下载队列为空时，调度器将会自动暂停以节省性能，并在任务添加/结束时继续。<br/>
    /// 这样会导致一些静态参数 (如最大并发数量) 的更改不会立即生效，此时应调用该方法以手动激活调度器使更改生效。
    /// </summary>
    public void ActivateScheduler() => _taskEvent.Set();

    private CancellationTokenSource? _cancelTokenSource;

    /// <summary>
    /// 取消所有下载任务并停止调度器。
    /// </summary>
    /// <returns>若调度器未开始运行则返回 <c>false</c>，否则返回 <c>true</c></returns>
    public bool Cancel()
    {
        if (_cancelTokenSource == null) return false;
        _cancelTokenSource.Cancel();
        _taskEvent.Set();
        LogWrapper.Info(LogModule, $"#{SchedulerId} 已取消");
        return true;
    }

    private int _currentParallelCount = 0;

    private bool _CanStartNewParallel =>
        _currentParallelCount < MaxParallels && _currentParallelCount < ParallelTaskLimit;

    /// <summary>
    /// 启动调度器
    /// </summary>
    /// <param name="cancelToken">取消操作通知</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start(CancellationToken cancelToken = default)
    {
        if (_schedulerThread != null) throw new InvalidOperationException("Cannot start twice");
        _cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        var cToken = _cancelTokenSource.Token;
        _schedulerThread = new Thread(() =>
        {
            var dequeued = new List<DownloadItem>();
            while (!cToken.IsCancellationRequested)
            {
                if (_CanStartNewParallel && _downloadQueue.TryDequeue(out var item))
                {
                    if (item.Status is DownloadItemStatus.Cancelled or DownloadItemStatus.Success) continue;
                    dequeued.Add(item);
                    if (item.Status is not DownloadItemStatus.Starting) _StartNewParallel(item, cToken);
                }
                else
                {
                    try { Task.Delay(RefreshInterval, cToken).Wait(cToken); }
                    catch (Exception) { break; }
                    RunningItems = dequeued.ToArray();
                    dequeued.ForEach(i => _downloadQueue.Enqueue(i));
                    dequeued.Clear();
                }
                if (_downloadQueue.IsEmpty) _taskEvent.WaitOne();
            }
        }) {
            IsBackground = true,
            Name = $"Scheduler@{SchedulerId}"
        };
        _schedulerThread.Start();
    }

    private void _RunParallelTask(Task task)
    {
        _currentParallelCount++;
        // TODO limit threads
        Task.Run(async () =>
        {
            try { await task; }
            catch (Exception ex) { LogWrapper.Error(ex, LogModule, "下载任务执行出错"); }
            finally
            {
                _currentParallelCount--;
                _taskEvent.Set();
            }
        });
    }

    private void _StartNewParallel(DownloadItem item, CancellationToken cToken)
    {
        Task? task = null;
        if (item.Status == DownloadItemStatus.Waiting)
        {
            // 开始第一个分片 (也可能只有这一个 即不分片)
            task = item.NewSegment(0, null, ErrorCallback);
        }
        else for (
            var currentNode = item.Segments.First;
            currentNode != null && !cToken.IsCancellationRequested;
            currentNode = currentNode.Next)
        {
            // 分析正在运行的分片
            var seg = currentNode.Value;
            if (seg.Status != DownloadSegmentStatus.Running) continue;
            if (seg.CurrentChunkStartTime - DateTime.Now > Timeout)
            {
                // 超时重试
                task = item.RestartSegment(currentNode, true);
                break;
            }
            // 估计下载速度 防止重复下载同样的内容
            var transferLengthByTimeout = seg.ChunkSize / seg.LastChunkElapsedTime.Ticks * Timeout.Ticks;
            if (seg.RemainingLength <= transferLengthByTimeout) continue;
            // 开始新的分片
            var start = (seg.NextPosition + seg.EndPosition) / 2;
            var end = seg.EndPosition;
            task = item.NewSegment(start, end, ErrorCallback, currentNode);
            break;
        }
        if (task != null) _RunParallelTask(task);
        return;

        void ErrorCallback(DownloadSegmentStatus status, Exception? lastException)
        {
            if (status == DownloadSegmentStatus.FailedNotSupportRange)
            {
                item.TrySegment = false;
                return;
            }
            var statusCode = (int)status;
            LogWrapper.Warn(lastException, LogModule, $"下载失败 ({statusCode}): {item}");
            item.Cancel(true);
        }
    }
}
