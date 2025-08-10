using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net;

/// <summary>
/// 下载分片的状态。<br/>
/// 大于或等于 <see cref="Failed"/> 的状态都表示失败，此时的值可能是一个 HTTP 状态码。
/// </summary>
public enum DownloadSegmentStatus
{
    /// <summary>
    /// 任务未开始。
    /// </summary>
    
    WaitingStart,
    
    /// <summary>
    /// 正在等待服务器响应。
    /// </summary>
    WaitingServer,
    
    /// <summary>
    /// 正在传输数据。
    /// </summary>
    Running,
    
    /// <summary>
    /// 任务完成。
    /// </summary>
    Success,
    
    /// <summary>
    /// 任务已取消。
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// 任务失败。
    /// </summary>
    Failed,
    
    /// <summary>
    /// 服务器不支持分块。
    /// </summary>
    FailedNotSupportRange,
}

public class SegmentContentTooShortException(long expected, long actual)
    : Exception($"Too short content: expected {expected}, actual {actual}")
{
    public long ExpectedLength { get; } = expected;
    public long ActualLength { get; } = actual;
}

/// <summary>
/// 并行下载分片。
/// </summary>
public class DownloadSegment(Uri sourceUri, string targetPath, int chunkSize = 16384)
{
    /// <summary>
    /// 起始位置，若非分块下载可以不设置。
    /// </summary>
    public long StartPosition { get; private set; } = 0;

    /// <summary>
    /// 结束位置，若非分块下载可以不设置。
    /// </summary>
    public long EndPosition { get; set; } = 0;

    /// <summary>
    /// 下一次或正在传输的位置，将会随时更新。
    /// </summary>
    public long NextPosition { get; private set; } = 0;
    
    /// <summary>
    /// 单次块大小，写入流的缓冲区和单次传输的字节数均会使用该大小。
    /// </summary>
    public int ChunkSize { get; } = chunkSize;
    
    /// <summary>
    /// 当前块的传输开始时间。
    /// </summary>
    public DateTime CurrentChunkStartTime { get; private set; } = DateTime.Now;
    
    /// <summary>
    /// 上一个块的传输耗时。
    /// </summary>
    public TimeSpan LastChunkElapsedTime { get; private set; } = TimeSpan.Zero;
    
    /// <summary>
    /// 来源 URI。
    /// </summary>
    public Uri SourceUri { get; } = sourceUri;
    
    /// <summary>
    /// 实际 URI，正常情况下与 <see cref="SourceUri"/> 相同，若服务器响应了重定向则为重定向后的最终 URI。
    /// </summary>
    public Uri RealUri { get; private set; } = sourceUri;
    
    /// <summary>
    /// 要写入的目标文件路径。
    /// </summary>
    public string TargetPath { get; } = targetPath;
    
    /// <summary>
    /// 此并行任务结束时的回调，成功或失败结果均会触发该回调。
    /// </summary>
    public Action<DownloadSegment>? EndCallback { get; set; }
    
    /// <summary>
    /// 重试计数。当传输过程中出现预料之外的错误时，将会触发重试并减少此计数，直至减为 0。
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// 传输总长度。若来源不支持内容长度，则为 0。
    /// </summary>
    public long TotalLength => (EndPosition == 0) ? 0 : EndPosition - StartPosition + 1;
    
    /// <summary>
    /// 已传输的长度。
    /// </summary>
    public long TransferredLength => NextPosition - StartPosition;
    
    /// <summary>
    /// 剩余未传输长度。
    /// </summary>
    public long RemainingLength => EndPosition - NextPosition + 1;
    
    private DownloadSegmentStatus _status = DownloadSegmentStatus.WaitingStart;
    
    /// <summary>
    /// 任务当前状态改变时触发的事件。
    /// </summary>
    public event Action<DownloadSegmentStatus>? StatusChanged;

    /// <summary>
    /// 任务当前状态，将会随时更新。
    /// </summary>
    public DownloadSegmentStatus Status
    {
        get => _status;
        private set
        {
            _status = value;
            StatusChanged?.Invoke(_status);
        }
    }

    /// <summary>
    /// 快速订阅改变至某个状态的事件。
    /// </summary>
    /// <param name="when">目标状态</param>
    /// <param name="action">事件回调委托</param>
    public void When(DownloadSegmentStatus when, Action action)
    {
        if (Status >= when) return;
        StatusChanged += TempHandler;
        return;

        void TempHandler(DownloadSegmentStatus status)
        {
            if (status < when) return;
            StatusChanged -= TempHandler;
            if (status == when) action();
        }
    }
    
    /// <summary>
    /// 导致上次传输失败的异常，若传输成功则为 <c>null</c>。
    /// </summary>
    public Exception? LastException { get; private set; }

    private CancellationTokenSource? _cancelTokenSource;

    public async Task Start(bool enableRange, long startPosition = 0, CancellationToken cancelToken = default)
    {
        _cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        var cToken = _cancelTokenSource.Token;
        cToken.Register(() => Status = DownloadSegmentStatus.Cancelled);
        // 根据重试计数开始传输文件
        var failedStatus = (int)DownloadSegmentStatus.Failed;
        while (RetryCount > 0 && !cToken.IsCancellationRequested)
        {
            try
            {
                using var stream = new FileStream(TargetPath,
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, ChunkSize, true);
                // 发送 HTTP 请求并等待响应
                Status = DownloadSegmentStatus.WaitingServer;
                var req = new HttpRequestMessage(HttpMethod.Get, SourceUri);
                if (enableRange)
                {
                    req.Headers.Range = new RangeHeaderValue(startPosition, null);
                    StartPosition = startPosition;
                    NextPosition = startPosition;
                }
                using var resp = await HttpRequest
                    .SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cToken);
                RealUri = resp.RequestMessage.RequestUri;
                var status = resp.StatusCode;
                failedStatus = (int)status;
                resp.EnsureSuccessStatusCode();
                // 分析响应头
                var headers = resp.Content.Headers;
                if (enableRange)
                {
                    var range = headers.ContentRange;
                    if (status == HttpStatusCode.PartialContent && range.HasRange)
                    {
                        // 尝试设置内容结束位置
                        if (EndPosition == 0)
                        {
                            if (range.To is { } end) EndPosition = end;
                            else if (range.Length is { } len) EndPosition = len - 1;
                        }
                    }
                    else
                    {
                        Status = DownloadSegmentStatus.FailedNotSupportRange;
                        break;
                    }
                }
                if (EndPosition == 0 && headers.ContentLength is { } length)
                {
                    // 设置内容结束位置
                    EndPosition = StartPosition + length - 1;
                }
                // 开始传输
                Status = DownloadSegmentStatus.Running;
                using var httpContentStream = await resp.Content.ReadAsStreamAsync();
                stream.Position = StartPosition;
                if (EndPosition == 0)
                {
                    var totalRead = 0L;
                    while (!cToken.IsCancellationRequested)
                    {
                        var buffer = new byte[ChunkSize];
                        CurrentChunkStartTime = DateTime.Now;
                        var read = await httpContentStream.ReadAsync(buffer, 0, ChunkSize, cToken);
                        await stream.WriteAsync(buffer, 0, read, cToken);
                        var endTime = DateTime.Now;
                        LastChunkElapsedTime = endTime - CurrentChunkStartTime;
                        totalRead += read;
                        if (read < ChunkSize) break;
                    }
                    NextPosition = StartPosition + totalRead;
                    EndPosition = NextPosition - 1;
                }
                else
                {
                    long remaining;
                    while (!cToken.IsCancellationRequested && (remaining = RemainingLength) > 0)
                    {
                        var count = remaining > ChunkSize ? ChunkSize : (int)remaining;
                        var buffer = new byte[count];
                        CurrentChunkStartTime = DateTime.Now;
                        var read = await httpContentStream.ReadAsync(buffer, 0, count, cToken);
                        var endTime = DateTime.Now;
                        LastChunkElapsedTime = endTime - CurrentChunkStartTime;
                        if (read < count) throw new SegmentContentTooShortException(TotalLength, TransferredLength + read);
                        await stream.WriteAsync(buffer, 0, read, cToken);
                        NextPosition += read;
                    }
                }
                await stream.FlushAsync(cToken);
                if (!cToken.IsCancellationRequested) Status = DownloadSegmentStatus.Success;
                break;
            }
            catch (Exception e)
            {
                LastException = e;
                if (--RetryCount == 0) Status = (DownloadSegmentStatus)(failedStatus);
                else await Task.Delay(1000, cToken);
            }
        }
        EndCallback?.Invoke(this);
    }
    
    /// <summary>
    /// 取消此分片的下载。
    /// </summary>
    /// <returns>若已开始下载则为 <c>true</c>，否则为 <c>false</c></returns>
    public bool Cancel()
    {
        if (_cancelTokenSource == null) return false;
        _cancelTokenSource.Cancel();
        return true;
    }
    
    public override string ToString() => $"[{SourceUri}]({StartPosition}..{EndPosition}) -> [{TargetPath}]";
}
