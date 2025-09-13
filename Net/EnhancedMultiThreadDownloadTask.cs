using System;
using System.Buffers;
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
/// 增强的多线程下载器配置
/// </summary>
public class DownloadConfiguration
{
    /// <summary>
    /// 线程数量
    /// </summary>
    public int ThreadCount { get; set; } = 4;
    
    /// <summary>
    /// 分块大小
    /// </summary>
    public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
    
    /// <summary>
    /// 缓冲区大小
    /// </summary>
    public int BufferSize { get; set; } = 81920; // 80KB
    
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// 超时时间(毫秒)
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// 启用断点续传
    /// </summary>
    public bool EnableResumeSupport { get; set; } = true;
    
    /// <summary>
    /// 速度限制 (字节/秒，0表示无限制)
    /// </summary>
    public long SpeedLimit { get; set; } = 0;
    
    /// <summary>
    /// 启用连接池复用
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;
    
    /// <summary>
    /// 文件预分配
    /// </summary>
    public bool PreAllocateFile { get; set; } = true;
    
    /// <summary>
    /// 启用详细日志
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}

/// <summary>
/// 下载统计信息
/// </summary>
public class DownloadStatistics
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double CurrentSpeed { get; set; } // bytes/second
    public double AverageSpeed { get; set; } // bytes/second
    public double PeakSpeed { get; set; } // bytes/second
    public int ActiveThreads { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
    public double ProgressPercentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
    public TimeSpan EstimatedTimeRemaining => CurrentSpeed > 0 ? 
        TimeSpan.FromSeconds((TotalBytes - DownloadedBytes) / CurrentSpeed) : TimeSpan.Zero;
}

/// <summary>
/// 速度控制器
/// </summary>
public class SpeedController
{
    private readonly long _speedLimit;
    private readonly SemaphoreSlim _semaphore;
    private DateTime _lastCheck = DateTime.Now;
    private long _bytesInCurrentSecond = 0;
    
    public SpeedController(long speedLimit)
    {
        _speedLimit = speedLimit;
        _semaphore = new SemaphoreSlim(1, 1);
    }
    
    public async Task<bool> CanTransfer(int bytes, CancellationToken cancellationToken)
    {
        if (_speedLimit <= 0) return true;
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastCheck).TotalMilliseconds;
            
            if (elapsed >= 1000) // Reset every second
            {
                _lastCheck = now;
                _bytesInCurrentSecond = 0;
            }
            
            if (_bytesInCurrentSecond + bytes <= _speedLimit)
            {
                _bytesInCurrentSecond += bytes;
                return true;
            }
            
            // Need to wait
            var remainingTime = 1000 - elapsed;
            if (remainingTime > 0)
            {
                await Task.Delay((int)remainingTime, cancellationToken);
                _lastCheck = DateTime.Now;
                _bytesInCurrentSecond = bytes;
            }
            
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// 增强的多线程下载任务
/// 支持断点续传、速度限制、连接池复用等高级功能
/// </summary>
public class EnhancedMultiThreadDownloadTask : TaskBase<MultiThreadDownloadResult>
{
    private const string LogModule = "EnhancedMultiThreadDownload";
    
    private readonly Uri _sourceUri;
    private readonly string _targetPath;
    private readonly string _tempPath;
    private readonly DownloadConfiguration _config;
    private readonly DownloadStatistics _stats = new();
    private readonly SpeedController _speedController;
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    
    private long _totalSize = 0;
    private long _totalDownloaded = 0;
    private readonly ConcurrentQueue<DownloadChunk> _pendingChunks = new();
    private readonly ConcurrentDictionary<int, DownloadChunk> _activeChunks = new();
    private readonly object _statsLock = new();
    private CancellationTokenSource? _internalCts;
    private Timer? _speedCalculationTimer;
    
    // 性能计数器
    private long _lastSpeedCheck = 0;
    private DateTime _lastSpeedTime = DateTime.Now;
    
    public DownloadStatistics Statistics => _stats;
    
    /// <summary>
    /// 创建增强的多线程下载任务
    /// </summary>
    public EnhancedMultiThreadDownloadTask(
        Uri sourceUri,
        string targetPath,
        DownloadConfiguration? config = null,
        CancellationToken? cancellationToken = null)
        : base($"增强下载 {Path.GetFileName(targetPath)}", cancellationToken, $"从 {sourceUri} 下载到 {targetPath}")
    {
        _sourceUri = sourceUri ?? throw new ArgumentNullException(nameof(sourceUri));
        _targetPath = Path.GetFullPath(targetPath);
        _tempPath = _targetPath + ".download";
        _config = config ?? new DownloadConfiguration();
        _speedController = new SpeedController(_config.SpeedLimit);
        
        // 验证配置
        ValidateConfiguration();
        
        LogWrapper.Info(LogModule, $"创建增强下载任务: {Name}, 配置: {_config.ThreadCount}线程, {_config.ChunkSize / 1024 / 1024}MB分块");
    }
    
    private void ValidateConfiguration()
    {
        _config.ThreadCount = Math.Max(1, Math.Min(_config.ThreadCount, 32));
        _config.ChunkSize = Math.Max(64 * 1024, _config.ChunkSize); // 最小64KB
        _config.BufferSize = Math.Max(4096, _config.BufferSize); // 最小4KB
        _config.MaxRetries = Math.Max(0, _config.MaxRetries);
        _config.TimeoutMs = Math.Max(5000, _config.TimeoutMs); // 最小5秒
    }
    
    public override async Task<MultiThreadDownloadResult> RunAsync(params object[] objects)
    {
        if (State != TaskState.Waiting)
            throw new InvalidOperationException($"任务已执行，当前状态: {State}");
            
        State = TaskState.Running;
        Progress = 0;
        _stats.StartTime = DateTime.Now;
        
        try
        {
            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken ?? System.Threading.CancellationToken.None);
            var token = _internalCts.Token;
            
            LogWrapper.Info(LogModule, $"开始增强下载: {_sourceUri} -> {_targetPath}");
            
            // 启动速度计算定时器
            StartSpeedCalculationTimer();
            
            // 1. 检查断点续传
            Progress = 0.02;
            var resumeInfo = await CheckResumeCapabilityAsync(token);
            
            // 2. 获取文件信息
            Progress = 0.05;
            var fileInfo = await GetFileInfoAsync(token);
            
            if (!fileInfo.SupportsRange || fileInfo.ContentLength <= _config.ChunkSize)
            {
                LogWrapper.Info(LogModule, "使用单线程下载模式");
                return await DownloadSingleThreadAsync(token);
            }
            
            _totalSize = fileInfo.ContentLength;
            _stats.TotalBytes = _totalSize;
            
            // 3. 处理断点续传或创建新下载
            Progress = 0.08;
            if (resumeInfo.CanResume && resumeInfo.ExistingSize > 0)
            {
                LogWrapper.Info(LogModule, $"断点续传: 已下载 {resumeInfo.ExistingSize:N0} / {_totalSize:N0} bytes");
                Interlocked.Exchange(ref _totalDownloaded, resumeInfo.ExistingSize);
                CreateResumeChunks(resumeInfo.ExistingSize, _totalSize);
            }
            else
            {
                CreateDownloadChunks(_totalSize);
                PrepareTargetFile();
            }
            
            // 4. 执行多线程下载
            Progress = 0.1;
            await DownloadMultiThreadAsync(token);
            
            // 5. 验证和完成
            Progress = 0.95;
            await FinalizeDownloadAsync(token);
            
            Progress = 1.0;
            State = TaskState.Completed;
            _stats.EndTime = DateTime.Now;
            
            var result = CreateSuccessResult();
            LogWrapper.Info(LogModule, $"下载完成: 大小={_totalSize:N0} bytes, 耗时={_stats.ElapsedTime.TotalSeconds:F1}s, 平均速度={result.AverageSpeed / 1024 / 1024:F2}MB/s");
            return result;
        }
        catch (OperationCanceledException)
        {
            State = TaskState.Canceled;
            LogWrapper.Info(LogModule, $"下载已取消: {_targetPath}");
            return CreateCancelledResult();
        }
        catch (Exception ex)
        {
            State = TaskState.Failed;
            LogWrapper.Error(ex, LogModule, $"下载失败: {_targetPath}");
            return CreateFailedResult(ex);
        }
        finally
        {
            _speedCalculationTimer?.Dispose();
            _internalCts?.Dispose();
        }
    }
    
    /// <summary>
    /// 检查断点续传能力
    /// </summary>
    private async Task<(bool CanResume, long ExistingSize)> CheckResumeCapabilityAsync(CancellationToken token)
    {
        if (!_config.EnableResumeSupport)
            return (false, 0);
            
        try
        {
            if (File.Exists(_tempPath))
            {
                var existingSize = new FileInfo(_tempPath).Length;
                if (existingSize > 0)
                {
                    // 验证服务器是否支持Range请求
                    using var client = GetHttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Head, _sourceUri);
                    using var response = await client.SendAsync(request, token);
                    
                    var supportsRange = response.Headers.AcceptRanges?.Contains("bytes") == true;
                    if (supportsRange)
                    {
                        LogWrapper.Info(LogModule, $"检测到断点续传文件: {existingSize:N0} bytes");
                        return (true, existingSize);
                    }
                }
            }
            else if (File.Exists(_targetPath))
            {
                // 目标文件已存在，检查是否完整
                var existingSize = new FileInfo(_targetPath).Length;
                using var client = GetHttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Head, _sourceUri);
                using var response = await client.SendAsync(request, token);
                
                var serverSize = response.Content.Headers.ContentLength ?? 0;
                if (existingSize == serverSize)
                {
                    LogWrapper.Info(LogModule, "文件已完整存在，跳过下载");
                    return (true, existingSize);
                }
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(LogModule, $"检查断点续传失败: {ex.Message}");
        }
        
        return (false, 0);
    }
    
    /// <summary>
    /// 创建断点续传的下载分块
    /// </summary>
    private void CreateResumeChunks(long existingSize, long totalSize)
    {
        if (existingSize >= totalSize)
        {
            // 文件已完整
            Interlocked.Exchange(ref _totalDownloaded, totalSize);
            return;
        }
        
        var remainingSize = totalSize - existingSize;
        var chunkCount = Math.Min(_config.ThreadCount, (int)Math.Ceiling((double)remainingSize / _config.ChunkSize));
        var chunkSize = remainingSize / chunkCount;
        
        for (int i = 0; i < chunkCount; i++)
        {
            var start = existingSize + (i * chunkSize);
            var end = i == chunkCount - 1 ? totalSize - 1 : start + chunkSize - 1;
            
            var chunk = new DownloadChunk
            {
                Id = i,
                StartPosition = start,
                EndPosition = end,
                Status = ChunkStatus.Waiting
            };
            
            _pendingChunks.Enqueue(chunk);
        }
        
        LogWrapper.Info(LogModule, $"断点续传: 创建 {chunkCount} 个分块下载剩余 {remainingSize:N0} bytes");
    }
    
    /// <summary>
    /// 获取优化的HttpClient
    /// </summary>
    private HttpClient GetHttpClient()
    {
        if (_config.EnableConnectionPooling)
        {
            return NetworkService.GetClient();
        }
        else
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(_config.TimeoutMs);
            return client;
        }
    }
    
    /// <summary>
    /// 优化的多线程下载
    /// </summary>
    private async Task DownloadMultiThreadAsync(CancellationToken token)
    {
        var downloadTasks = new List<Task>();
        
        // 启动工作线程
        for (int i = 0; i < _config.ThreadCount; i++)
        {
            var threadId = i;
            var task = Task.Run(async () => await OptimizedDownloadWorkerAsync(threadId, token), token);
            downloadTasks.Add(task);
        }
        
        // 启动进度更新任务
        var progressTask = Task.Run(async () => await UpdateProgressAsync(token), token);
        downloadTasks.Add(progressTask);
        
        // 等待所有任务完成
        await Task.WhenAll(downloadTasks);
    }
    
    /// <summary>
    /// 优化的下载工作线程
    /// </summary>
    private async Task OptimizedDownloadWorkerAsync(int workerId, CancellationToken token)
    {
        using var client = GetHttpClient();
        var buffer = _bufferPool.Rent(_config.BufferSize);
        
        try
        {
            if (_config.VerboseLogging)
                LogWrapper.Debug(LogModule, $"工作线程 {workerId} 启动 (缓冲区: {_config.BufferSize:N0} bytes)");
                
            while (!token.IsCancellationRequested && _pendingChunks.TryDequeue(out var chunk))
            {
                _activeChunks[workerId] = chunk;
                await DownloadChunkOptimizedAsync(client, chunk, buffer, workerId, token);
                _activeChunks.TryRemove(workerId, out _);
            }
            
            if (_config.VerboseLogging)
                LogWrapper.Debug(LogModule, $"工作线程 {workerId} 完成");
        }
        finally
        {
            _bufferPool.Return(buffer);
            if (!_config.EnableConnectionPooling)
            {
                client.Dispose();
            }
        }
    }
    
    /// <summary>
    /// 优化的分块下载
    /// </summary>
    private async Task DownloadChunkOptimizedAsync(HttpClient client, DownloadChunk chunk, byte[] buffer, int workerId, CancellationToken token)
    {
        for (int attempt = 0; attempt <= _config.MaxRetries; attempt++)
        {
            if (token.IsCancellationRequested) return;
            
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
                
                // 使用异步文件IO
                using var fileStream = new FileStream(
                    _tempPath, 
                    FileMode.OpenOrCreate, 
                    FileAccess.Write, 
                    FileShare.Write,
                    bufferSize: 4096,
                    useAsync: true);
                
                fileStream.Position = chunk.StartPosition + chunk.DownloadedBytes;
                
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    // 速度限制检查
                    await _speedController.CanTransfer(bytesRead, token);
                    
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    await fileStream.FlushAsync(token);
                    
                    // 更新统计信息
                    UpdateDownloadProgress(chunk, bytesRead);
                }
                
                chunk.Status = ChunkStatus.Completed;
                
                if (_config.VerboseLogging)
                    LogWrapper.Debug(LogModule, $"线程 {workerId} 完成分块 {chunk.Id}");
                    
                return;
            }
            catch (Exception ex) when (attempt < _config.MaxRetries)
            {
                chunk.LastError = ex;
                chunk.RetryCount = attempt + 1;
                _stats.RetryCount++;
                
                LogWrapper.Warn(LogModule, $"线程 {workerId} 分块 {chunk.Id} 重试 {attempt + 1}/{_config.MaxRetries}: {ex.Message}");
                
                // 指数退避
                var delay = Math.Min(1000 * (int)Math.Pow(2, attempt), 10000);
                await Task.Delay(delay, token);
            }
            catch (Exception ex)
            {
                chunk.Status = ChunkStatus.Failed;
                chunk.LastError = ex;
                LogWrapper.Error(ex, LogModule, $"线程 {workerId} 分块 {chunk.Id} 最终失败");
                throw;
            }
        }
    }
    
    /// <summary>
    /// 更新下载进度
    /// </summary>
    private void UpdateDownloadProgress(DownloadChunk chunk, int bytesRead)
    {
        lock (_statsLock)
        {
            chunk.DownloadedBytes += bytesRead;
            Interlocked.Add(ref _totalDownloaded, bytesRead);
            
            // 更新统计
            _stats.DownloadedBytes = Interlocked.Read(ref _totalDownloaded);
            _stats.ActiveThreads = _activeChunks.Count;
        }
    }
    
    /// <summary>
    /// 启动速度计算定时器
    /// </summary>
    private void StartSpeedCalculationTimer()
    {
        _speedCalculationTimer = new Timer(CalculateSpeed, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    /// <summary>
    /// 计算下载速度
    /// </summary>
    private void CalculateSpeed(object? state)
    {
        var now = DateTime.Now;
        var currentDownloaded = Interlocked.Read(ref _totalDownloaded);
        
        lock (_statsLock)
        {
            var timeDiff = (now - _lastSpeedTime).TotalSeconds;
            if (timeDiff > 0)
            {
                var bytesDiff = currentDownloaded - _lastSpeedCheck;
                _stats.CurrentSpeed = bytesDiff / timeDiff;
                
                // 更新峰值速度
                if (_stats.CurrentSpeed > _stats.PeakSpeed)
                    _stats.PeakSpeed = _stats.CurrentSpeed;
                
                // 计算平均速度
                var totalTime = _stats.ElapsedTime.TotalSeconds;
                if (totalTime > 0)
                    _stats.AverageSpeed = currentDownloaded / totalTime;
            }
            
            _lastSpeedCheck = currentDownloaded;
            _lastSpeedTime = now;
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
                if (_totalSize > 0)
                {
                    var totalDownloaded = Interlocked.Read(ref _totalDownloaded);
                    var currentProgress = 0.1 + (0.85 * totalDownloaded / _totalSize);
                    Progress = Math.Min(currentProgress, 0.95);
                }
                
                await Task.Delay(200, token); // 每200ms更新一次
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// 获取文件信息
    /// </summary>
    private async Task<(bool SupportsRange, long ContentLength)> GetFileInfoAsync(CancellationToken token)
    {
        using var client = GetHttpClient();
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
        var chunkCount = Math.Min(_config.ThreadCount, (int)Math.Ceiling((double)totalSize / _config.ChunkSize));
        if (chunkCount <= 0)
        {
            LogWrapper.Error(LogModule, $"无法创建下载分块: chunkCount={chunkCount} (ThreadCount={_config.ThreadCount}, totalSize={totalSize}, ChunkSize={_config.ChunkSize})");
            return;
        }
        var chunkSize = totalSize / chunkCount;
        var remainder = totalSize % chunkCount;
        
        for (int i = 0; i < chunkCount; i++)
        {
            var start = i * chunkSize;
            var end = start + chunkSize - 1;
            if (i == chunkCount - 1)
                end += remainder;
                
            var chunk = new DownloadChunk
            {
                Id = i,
                StartPosition = start,
                EndPosition = end,
                Status = ChunkStatus.Waiting
            };
            
            _pendingChunks.Enqueue(chunk);
        }
        
        LogWrapper.Info(LogModule, $"创建 {chunkCount} 个下载分块");
    }
    
    /// <summary>
    /// 准备目标文件
    /// </summary>
    private void PrepareTargetFile()
    {
        var directory = Path.GetDirectoryName(_tempPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        if (_config.PreAllocateFile && _totalSize > 0)
        {
            using var fs = new FileStream(_tempPath, FileMode.Create, FileAccess.Write);
            fs.SetLength(_totalSize);
        }
    }
    
    /// <summary>
    /// 完成下载
    /// </summary>
    private async Task FinalizeDownloadAsync(CancellationToken token)
    {
        await Task.Run(() =>
        {
            // 验证文件完整性
            if (!File.Exists(_tempPath))
                throw new FileNotFoundException("临时下载文件不存在");
                
            var actualSize = new FileInfo(_tempPath).Length;
            if (actualSize != _totalSize)
                throw new InvalidDataException($"文件大小不匹配: 期望 {_totalSize:N0}, 实际 {actualSize:N0}");
            
            // 移动到最终位置
            if (File.Exists(_targetPath))
                File.Delete(_targetPath);
                
            File.Move(_tempPath, _targetPath);
            
            LogWrapper.Info(LogModule, "文件验证和移动完成");
        }, token);
    }
    
    /// <summary>
    /// 单线程下载
    /// </summary>
    private async Task<MultiThreadDownloadResult> DownloadSingleThreadAsync(CancellationToken token)
    {
        using var client = GetHttpClient();
        using var response = await client.GetAsync(_sourceUri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        
        _totalSize = response.Content.Headers.ContentLength ?? 0;
        _stats.TotalBytes = _totalSize;
        
        using var contentStream = await response.Content.ReadAsStreamAsync(token);
        using var fileStream = new FileStream(_targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        
        var buffer = _bufferPool.Rent(_config.BufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await _speedController.CanTransfer(bytesRead, token);
                await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                
                Interlocked.Add(ref _totalDownloaded, bytesRead);
                _stats.DownloadedBytes = Interlocked.Read(ref _totalDownloaded);
                
                if (_totalSize > 0)
                {
                    var totalDownloaded = Interlocked.Read(ref _totalDownloaded);
                    Progress = 0.1 + (0.85 * totalDownloaded / _totalSize);
                }
            }
        }
        finally
        {
            _bufferPool.Return(buffer);
        }
        
        _stats.EndTime = DateTime.Now;
        return CreateSuccessResult();
    }
    
    private MultiThreadDownloadResult CreateSuccessResult()
    {
        return new MultiThreadDownloadResult
        {
            FilePath = _targetPath,
            TotalSize = _totalSize,
            Duration = _stats.ElapsedTime,
            AverageSpeed = _stats.AverageSpeed,
            IsSuccess = true
        };
    }
    
    private MultiThreadDownloadResult CreateCancelledResult()
    {
        return new MultiThreadDownloadResult
        {
            FilePath = _targetPath,
            Duration = _stats.ElapsedTime,
            IsSuccess = false,
            ErrorMessage = "下载已取消"
        };
    }
    
    private MultiThreadDownloadResult CreateFailedResult(Exception ex)
    {
        return new MultiThreadDownloadResult
        {
            FilePath = _targetPath,
            Duration = _stats.ElapsedTime,
            IsSuccess = false,
            ErrorMessage = ex.Message
        };
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
    /// 获取详细状态信息
    /// </summary>
    public DownloadStatistics GetDetailedStatus()
    {
        lock (_statsLock)
        {
            return new DownloadStatistics
            {
                StartTime = _stats.StartTime,
                EndTime = _stats.EndTime,
                TotalBytes = _stats.TotalBytes,
                DownloadedBytes = _stats.DownloadedBytes,
                CurrentSpeed = _stats.CurrentSpeed,
                AverageSpeed = _stats.AverageSpeed,
                PeakSpeed = _stats.PeakSpeed,
                ActiveThreads = _stats.ActiveThreads,
                RetryCount = _stats.RetryCount
            };
        }
    }
}
