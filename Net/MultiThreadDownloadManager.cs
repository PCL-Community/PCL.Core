using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 多线程下载管理器
/// 提供简化的API用于管理多个下载任务
/// </summary>
public class MultiThreadDownloadManager : IDisposable
{
    private const string LogModule = "MultiThreadDownloadManager";
    
    private readonly ConcurrentDictionary<string, MultiThreadDownloadTask> _activeTasks = new();
    private readonly CancellationTokenSource _globalCts = new();
    private bool _disposed = false;
    
    /// <summary>
    /// 默认线程数
    /// </summary>
    public int DefaultThreadCount { get; set; } = 4;
    
    /// <summary>
    /// 默认分块大小 (字节)
    /// </summary>
    public int DefaultChunkSize { get; set; } = 1024 * 1024; // 1MB
    
    /// <summary>
    /// 默认最大重试次数
    /// </summary>
    public int DefaultMaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 默认超时时间 (毫秒)
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000; // 30秒
    
    /// <summary>
    /// 当前活动任务数量
    /// </summary>
    public int ActiveTaskCount => _activeTasks.Count;
    
    /// <summary>
    /// 获取所有活动任务
    /// </summary>
    public IEnumerable<MultiThreadDownloadTask> ActiveTasks => _activeTasks.Values.ToArray();
    
    /// <summary>
    /// 创建并启动下载任务
    /// </summary>
    /// <param name="url">下载地址</param>
    /// <param name="filePath">保存路径</param>
    /// <param name="threadCount">线程数(可选，使用默认值)</param>
    /// <param name="chunkSize">分块大小(可选，使用默认值)</param>
    /// <param name="maxRetries">最大重试次数(可选，使用默认值)</param>
    /// <param name="timeoutMs">超时时间(可选，使用默认值)</param>
    /// <param name="progressCallback">进度回调(可选)</param>
    /// <param name="stateCallback">状态变化回调(可选)</param>
    /// <returns>下载任务</returns>
    public async Task<MultiThreadDownloadTask> DownloadAsync(
        string url,
        string filePath,
        int? threadCount = null,
        int? chunkSize = null,
        int? maxRetries = null,
        int? timeoutMs = null,
        Action<double>? progressCallback = null,
        Action<TaskState>? stateCallback = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MultiThreadDownloadManager));
            
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"无效的URL: {url}", nameof(url));
            
        var taskId = Path.GetFileName(filePath) ?? Guid.NewGuid().ToString();
        if (_activeTasks.ContainsKey(taskId))
        {
            taskId += "_" + DateTime.Now.Ticks;
        }
        
        var downloadTask = new MultiThreadDownloadTask(
            uri,
            filePath,
            threadCount ?? DefaultThreadCount,
            chunkSize ?? DefaultChunkSize,
            maxRetries ?? DefaultMaxRetries,
            timeoutMs ?? DefaultTimeoutMs,
            0, // speedLimitBytesPerSecond - 默认无限制
            1000, // baseRetryDelayMs - 默认1秒
            true, // useExponentialBackoff - 默认启用指数退避
            _globalCts.Token
        );
        
        // 订阅进度和状态变化事件
        if (progressCallback != null)
        {
            downloadTask.ProgressChanged += (sender, oldValue, newValue) => progressCallback(newValue);
        }
        
        if (stateCallback != null)
        {
            downloadTask.StateChanged += (sender, oldValue, newValue) => stateCallback(newValue);
        }
        
        // 任务完成后自动从活动列表中移除
        downloadTask.StateChanged += (sender, oldState, newState) =>
        {
            if (newState is TaskState.Completed or TaskState.Failed or TaskState.Canceled)
            {
                _activeTasks.TryRemove(taskId, out _);
                LogWrapper.Info(LogModule, $"任务已完成并从活动列表中移除: {taskId}");
            }
        };
        
        _activeTasks[taskId] = downloadTask;
        LogWrapper.Info(LogModule, $"开始下载任务: {taskId} [{url} -> {filePath}]");
        
        // 在后台运行任务
        _ = Task.Run(async () =>
        {
            try
            {
                await downloadTask.RunAsync();
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, LogModule, $"下载任务执行异常: {taskId}");
            }
        }, _globalCts.Token);
        
        return downloadTask;
    }
    
    /// <summary>
    /// 同步下载文件 (阻塞直到完成)
    /// </summary>
    /// <param name="url">下载地址</param>
    /// <param name="filePath">保存路径</param>
    /// <param name="threadCount">线程数(可选)</param>
    /// <param name="chunkSize">分块大小(可选)</param>
    /// <param name="maxRetries">最大重试次数(可选)</param>
    /// <param name="timeoutMs">超时时间(可选)</param>
    /// <param name="progressCallback">进度回调(可选)</param>
    /// <returns>下载结果</returns>
    public async Task<MultiThreadDownloadResult> DownloadFileAsync(
        string url,
        string filePath,
        int? threadCount = null,
        int? chunkSize = null,
        int? maxRetries = null,
        int? timeoutMs = null,
        Action<double>? progressCallback = null)
    {
        var task = await DownloadAsync(url, filePath, threadCount, chunkSize, maxRetries, timeoutMs, progressCallback);
        
        // 等待任务完成
        while (task.State is TaskState.Waiting or TaskState.Running)
        {
            await Task.Delay(100);
        }
        
        return task.Result ?? new MultiThreadDownloadResult
        {
            FilePath = filePath,
            IsSuccess = false,
            ErrorMessage = "任务未正确完成"
        };
    }
    
    /// <summary>
    /// 批量下载文件
    /// </summary>
    /// <param name="downloads">下载项列表(URL, 文件路径)</param>
    /// <param name="maxConcurrency">最大并发数</param>
    /// <param name="threadCountPerTask">每个任务的线程数</param>
    /// <param name="progressCallback">整体进度回调</param>
    /// <returns>所有下载结果</returns>
    public async Task<List<MultiThreadDownloadResult>> DownloadBatchAsync(
        IEnumerable<(string Url, string FilePath)> downloads,
        int maxConcurrency = 3,
        int threadCountPerTask = 2,
        Action<double>? progressCallback = null)
    {
        var downloadList = downloads.ToList();
        if (!downloadList.Any())
            return new List<MultiThreadDownloadResult>();
            
        var results = new ConcurrentBag<MultiThreadDownloadResult>();
        var completed = 0;
        
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = downloadList.Select(async download =>
        {
            await semaphore.WaitAsync(_globalCts.Token);
            try
            {
                var result = await DownloadFileAsync(
                    download.Url, 
                    download.FilePath, 
                    threadCountPerTask);
                    
                results.Add(result);
                
                var currentCompleted = Interlocked.Increment(ref completed);
                var overallProgress = (double)currentCompleted / downloadList.Count;
                progressCallback?.Invoke(overallProgress);
                
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        return results.ToList();
    }
    
    /// <summary>
    /// 取消指定任务
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>是否成功取消</returns>
    public bool CancelTask(string taskId)
    {
        if (_activeTasks.TryGetValue(taskId, out var task))
        {
            task.CancelDownload();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 取消所有活动任务
    /// </summary>
    public void CancelAllTasks()
    {
        LogWrapper.Info(LogModule, $"取消所有活动任务，共 {_activeTasks.Count} 个");
        
        foreach (var task in _activeTasks.Values)
        {
            task.CancelDownload();
        }
        
        _globalCts.Cancel();
    }
    
    /// <summary>
    /// 获取总体下载统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public (int ActiveTasks, long TotalDownloaded, long TotalSize, double OverallSpeed) GetOverallStatistics()
    {
        var activeTasks = _activeTasks.Values.ToArray();
        var totalDownloaded = 0L;
        var totalSize = 0L;
        var totalSpeed = 0.0;
        
        foreach (var task in activeTasks)
        {
            var (downloaded, total, speed, _) = task.GetDownloadStatus();
            totalDownloaded += downloaded;
            totalSize += total;
            totalSpeed += speed;
        }
        
        return (activeTasks.Length, totalDownloaded, totalSize, totalSpeed);
    }
    
    /// <summary>
    /// 等待所有任务完成
    /// </summary>
    /// <param name="timeout">超时时间(可选)</param>
    /// <returns>是否所有任务都成功完成</returns>
    public async Task<bool> WaitForAllTasksAsync(TimeSpan? timeout = null)
    {
        var startTime = DateTime.Now;
        
        while (_activeTasks.Any())
        {
            if (timeout.HasValue && DateTime.Now - startTime > timeout.Value)
            {
                LogWrapper.Warn(LogModule, "等待所有任务完成超时");
                return false;
            }
            
            await Task.Delay(100);
        }
        
        LogWrapper.Info(LogModule, "所有任务已完成");
        return true;
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        CancelAllTasks();
        _globalCts.Dispose();
        _disposed = true;
        
        LogWrapper.Info(LogModule, "多线程下载管理器已释放");
        GC.SuppressFinalize(this);
    }
}
