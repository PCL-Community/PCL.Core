using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// 多线程下载任务结果
/// </summary>
public class MultiThreadDownloadResult
{
    public string FilePath { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public TimeSpan Duration { get; set; }
    public double AverageSpeed { get; set; } // bytes per second
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 下载分片信息
/// </summary>
public class DownloadChunk
{
    public int Id { get; set; }
    public long StartPosition { get; set; }
    public long EndPosition { get; set; }
    public long DownloadedBytes { get; set; }
    public ChunkStatus Status { get; set; } = ChunkStatus.Waiting;
    public Exception? LastError { get; set; }
    public int RetryCount { get; set; } = 0;
}

/// <summary>
/// 分片状态
/// </summary>
public enum ChunkStatus
{
    Waiting,
    Downloading,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// 高性能多线程下载任务
/// 继承TaskBase以完全适配PCL.Core架构，支持IObservableProgressSource接口
/// </summary>
public class MultiThreadDownloadTask : TaskBase<MultiThreadDownloadResult>
{
    private const string LogModule = "MultiThreadDownload";
    
    private readonly Uri _sourceUri;
    private readonly string _targetPath;
    private readonly int _threadCount;
    private readonly int _chunkSize;
    private readonly int _maxRetries;
    private readonly int _timeoutMs;
    
    private long _totalSize = 0;
    private long _totalDownloaded = 0;
    private readonly ConcurrentQueue<DownloadChunk> _chunks = new();
    private readonly ConcurrentDictionary<int, DownloadChunk> _activeChunks = new();
    private readonly object _progressLock = new();
    private DateTime _startTime;
    private CancellationTokenSource? _internalCts;
    
    /// <summary>
    /// 创建多线程下载任务
    /// </summary>
    /// <param name="sourceUri">下载源地址</param>
    /// <param name="targetPath">目标文件路径</param>
    /// <param name="threadCount">线程数量(默认4)</param>
    /// <param name="chunkSize">分块大小(默认1MB)</param>
    /// <param name="maxRetries">最大重试次数(默认3)</param>
    /// <param name="timeoutMs">超时时间毫秒(默认30秒)</param>
    /// <param name="cancellationToken">取消令牌</param>
    public MultiThreadDownloadTask(
        Uri sourceUri,
        string targetPath,
        int threadCount = 4,
        int chunkSize = 1024 * 1024, // 1MB
        int maxRetries = 3,
        int timeoutMs = 30000,
        CancellationToken? cancellationToken = null)
        : base($"下载 {Path.GetFileName(targetPath)}", cancellationToken, $"从 {sourceUri} 下载到 {targetPath}")
    {
        _sourceUri = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
        _targetPath = Path.GetFullPath(targetPath);
        _threadCount = Math.Max(1, Math.Min(threadCount, 16)); // 限制在1-16个线程
        _chunkSize = Math.Max(1024, chunkSize); // 至少1KB
        _maxRetries = Math.Max(0, maxRetries);
        _timeoutMs = Math.Max(5000, timeoutMs); // 至少5秒
        
        LogWrapper.Info(LogModule, $"创建多线程下载任务: {Name}, 线程数: {_threadCount}");
    }
    
    /// <summary>
    /// 执行下载
    /// </summary>
    public override async Task<MultiThreadDownloadResult> RunAsync(params object[] objects)
    {
        if (State != TaskState.Waiting)
            throw new InvalidOperationException($"任务已执行，当前状态: {State}");
            
        State = TaskState.Running;
        Progress = 0;
        _startTime = DateTime.Now;
        
        try
        {
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken ?? System.Threading.CancellationToken.None);
            var token = _internalCts.Token;
            
            LogWrapper.Info(LogModule, $"开始下载: {_sourceUri} -> {_targetPath}");
            
            // 1. 获取文件信息
            Progress = 0.05;
            var fileInfo = await GetFileInfoAsync(token);
            if (!fileInfo.SupportsRange || fileInfo.ContentLength <= 0)
            {
                // 不支持分块下载，使用单线程
                LogWrapper.Info(LogModule, "服务器不支持分块下载，使用单线程模式");
                return await DownloadSingleThreadAsync(token);
            }
            
            _totalSize = fileInfo.ContentLength;
            
            // 2. 创建下载分块
            Progress = 0.1;
            CreateDownloadChunks(fileInfo.ContentLength);
            
            // 3. 准备目标文件
            PrepareTargetFile();
            
            // 4. 启动多线程下载
            Progress = 0.15;
            await DownloadMultiThreadAsync(token);
            
            // 5. 验证下载完整性
            Progress = 0.95;
            await ValidateDownloadAsync(token);
            
            Progress = 1.0;
            State = TaskState.Completed;
            
            var duration = DateTime.Now - _startTime;
            var result = new MultiThreadDownloadResult
            {
                FilePath = _targetPath,
                TotalSize = _totalSize,
                Duration = duration,
                AverageSpeed = _totalSize / duration.TotalSeconds,
                IsSuccess = true
            };
            
            LogWrapper.Info(LogModule, $"下载完成: {_targetPath}, 大小: {_totalSize:N0} bytes, 耗时: {duration.TotalSeconds:F1}s, 平均速度: {result.AverageSpeed / 1024 / 1024:F2} MB/s");
            return result;
        }
        catch (OperationCanceledException)
        {
            State = TaskState.Canceled;
            LogWrapper.Info(LogModule, $"下载已取消: {_targetPath}");
            return new MultiThreadDownloadResult
            {
                FilePath = _targetPath,
                Duration = DateTime.Now - _startTime,
                IsSuccess = false,
                ErrorMessage = "下载已取消"
            };
        }
        catch (Exception ex)
        {
            State = TaskState.Failed;
            LogWrapper.Error(ex, LogModule, $"下载失败: {_targetPath}");
            return new MultiThreadDownloadResult
            {
                FilePath = _targetPath,
                Duration = DateTime.Now - _startTime,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _internalCts?.Dispose();
        }
    }
    
    /// <summary>
    /// 获取文件信息
    /// </summary>
    private async Task<(bool SupportsRange, long ContentLength)> GetFileInfoAsync(CancellationToken token)
    {
        using var client = NetworkService.GetClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, _sourceUri);
        
        using var response = await client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        
        var supportsRange = response.Headers.AcceptRanges?.Contains("bytes") == true;
        var contentLength = response.Content.Headers.ContentLength ?? 0;
        
        LogWrapper.Info(LogModule, $"文件信息: 大小={contentLength:N0} bytes, 支持分块={supportsRange}");
        return (supportsRange, contentLength);
    }
    
    /// <summary>
    /// 创建下载分块
    /// </summary>
    private void CreateDownloadChunks(long totalSize)
    {
        var chunkCount = Math.Min(_threadCount, (int)Math.Ceiling((double)totalSize / _chunkSize));
        var chunkSize = totalSize / chunkCount;
        var remainder = totalSize % chunkCount;
        
        for (int i = 0; i < chunkCount; i++)
        {
            var start = i * chunkSize;
            var end = start + chunkSize - 1;
            if (i == chunkCount - 1) // 最后一块包含余数
                end += remainder;
                
            var chunk = new DownloadChunk
            {
                Id = i,
                StartPosition = start,
                EndPosition = end,
                Status = ChunkStatus.Waiting
            };
            
            _chunks.Enqueue(chunk);
        }
        
        LogWrapper.Info(LogModule, $"创建了 {chunkCount} 个下载分块，每块大小约 {chunkSize:N0} bytes");
    }
    
    /// <summary>
    /// 准备目标文件
    /// </summary>
    private void PrepareTargetFile()
    {
        var directory = Path.GetDirectoryName(_targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 预分配文件空间
        using var fs = new FileStream(_targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        fs.SetLength(_totalSize);
        fs.Close();
    }
    
    /// <summary>
    /// 多线程下载
    /// </summary>
    private async Task DownloadMultiThreadAsync(CancellationToken token)
    {
        var downloadTasks = new List<Task>();
        
        // 启动下载线程
        for (int i = 0; i < _threadCount; i++)
        {
            var threadId = i;
            var task = Task.Run(async () => await DownloadWorkerAsync(threadId, token), token);
            downloadTasks.Add(task);
        }
        
        // 启动进度更新任务
        var progressTask = Task.Run(async () => await UpdateProgressAsync(token), token);
        downloadTasks.Add(progressTask);
        
        // 等待所有任务完成
        await Task.WhenAll(downloadTasks);
    }
    
    /// <summary>
    /// 下载工作线程
    /// </summary>
    private async Task DownloadWorkerAsync(int workerId, CancellationToken token)
    {
        using var client = NetworkService.GetClient();
        client.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);
        
        LogWrapper.Debug(LogModule, $"工作线程 {workerId} 已启动");
        
        while (!token.IsCancellationRequested && _chunks.TryDequeue(out var chunk))
        {
            _activeChunks[workerId] = chunk;
            await DownloadChunkAsync(client, chunk, workerId, token);
            _activeChunks.TryRemove(workerId, out _);
        }
        
        LogWrapper.Debug(LogModule, $"工作线程 {workerId} 已完成");
    }
    
    /// <summary>
    /// 下载单个分块
    /// </summary>
    private async Task DownloadChunkAsync(HttpClient client, DownloadChunk chunk, int workerId, CancellationToken token)
    {
        var buffer = new byte[8192]; // 8KB读取缓冲区
        
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (token.IsCancellationRequested)
                return;
                
            try
            {
                chunk.Status = ChunkStatus.Downloading;
                chunk.LastError = null;
                
                using var request = new HttpRequestMessage(HttpMethod.Get, _sourceUri);
                request.Headers.Range = new RangeHeaderValue(
                    chunk.StartPosition + chunk.DownloadedBytes, 
                    chunk.EndPosition);
                
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                
                using var contentStream = await response.Content.ReadAsStreamAsync(token);
                using var fileStream = new FileStream(_targetPath, FileMode.Open, FileAccess.Write, FileShare.Write);
                
                fileStream.Position = chunk.StartPosition + chunk.DownloadedBytes;
                
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    
                    lock (_progressLock)
                    {
                        chunk.DownloadedBytes += bytesRead;
                        _totalDownloaded += bytesRead;
                    }
                }
                
                chunk.Status = ChunkStatus.Completed;
                LogWrapper.Debug(LogModule, $"工作线程 {workerId} 完成分块 {chunk.Id} ({chunk.StartPosition}-{chunk.EndPosition})");
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                chunk.LastError = ex;
                chunk.RetryCount = attempt + 1;
                LogWrapper.Warn(LogModule, $"工作线程 {workerId} 分块 {chunk.Id} 下载失败 (重试 {attempt + 1}/{_maxRetries}): {ex.Message}");
                
                // 重试前等待一段时间
                await Task.Delay(Math.Min(1000 * (attempt + 1), 5000), token);
            }
            catch (Exception ex)
            {
                chunk.Status = ChunkStatus.Failed;
                chunk.LastError = ex;
                LogWrapper.Error(ex, LogModule, $"工作线程 {workerId} 分块 {chunk.Id} 下载失败，已达最大重试次数");
                throw;
            }
        }
    }
    
    /// <summary>
    /// 更新进度
    /// </summary>
    private async Task UpdateProgressAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                lock (_progressLock)
                {
                    if (_totalSize > 0)
                    {
                        var currentProgress = 0.15 + (0.8 * _totalDownloaded / _totalSize); // 15%-95%的进度区间
                        Progress = Math.Min(currentProgress, 0.95);
                    }
                }
                
                await Task.Delay(100, token); // 每100ms更新一次进度
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// 单线程下载(用于不支持分块的服务器)
    /// </summary>
    private async Task<MultiThreadDownloadResult> DownloadSingleThreadAsync(CancellationToken token)
    {
        using var client = NetworkService.GetClient();
        using var response = await client.GetAsync(_sourceUri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        
        _totalSize = response.Content.Headers.ContentLength ?? 0;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(token);
        using var fileStream = new FileStream(_targetPath, FileMode.Create, FileAccess.Write);
        
        var buffer = new byte[8192];
        int bytesRead;
        long totalDownloaded = 0;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, token);
            totalDownloaded += bytesRead;
            
            if (_totalSize > 0)
            {
                Progress = 0.15 + (0.8 * totalDownloaded / _totalSize);
            }
        }
        
        Progress = 1.0;
        State = TaskState.Completed;
        
        var duration = DateTime.Now - _startTime;
        return new MultiThreadDownloadResult
        {
            FilePath = _targetPath,
            TotalSize = totalDownloaded,
            Duration = duration,
            AverageSpeed = totalDownloaded / duration.TotalSeconds,
            IsSuccess = true
        };
    }
    
    /// <summary>
    /// 验证下载完整性
    /// </summary>
    private async Task ValidateDownloadAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            var fileInfo = new FileInfo(_targetPath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("下载的文件不存在");
                
            if (fileInfo.Length != _totalSize)
                throw new InvalidDataException($"文件大小不匹配: 期望 {_totalSize:N0} bytes, 实际 {fileInfo.Length:N0} bytes");
                
            LogWrapper.Debug(LogModule, "文件完整性验证通过");
        }, token);
    }
    
    /// <summary>
    /// 取消下载
    /// </summary>
    public void CancelDownload()
    {
        _internalCts?.Cancel();
        LogWrapper.Info(LogModule, $"取消下载: {_targetPath}");
    }
    
    /// <summary>
    /// 获取当前下载状态信息
    /// </summary>
    public (long Downloaded, long Total, double Speed, int ActiveThreads) GetDownloadStatus()
    {
        lock (_progressLock)
        {
            var elapsed = DateTime.Now - _startTime;
            var speed = elapsed.TotalSeconds > 0 ? _totalDownloaded / elapsed.TotalSeconds : 0;
            return (_totalDownloaded, _totalSize, speed, _activeChunks.Count);
        }
    }
}
