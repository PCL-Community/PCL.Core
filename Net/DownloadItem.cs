using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net;

/// <summary>
/// 分块下载中断处理。
/// </summary>
/// <param name="status">任务状态</param>
/// <param name="lastException">任务中断前抛出的最后一个异常</param>
public delegate void SegmentInterruptHandler(DownloadSegmentStatus status, Exception? lastException);

/// <summary>
/// 下载项状态。
/// </summary>
public enum DownloadItemStatus
{
    Waiting,
    Starting,
    Running,
    Success,
    Cancelled,
    Failed,
}

/// <summary>
/// 下载项。
/// </summary>
public class DownloadItem(
    Uri uri,
    string targetPath,
    int chunkSize = 16384,
    int retry = 3)
{
    public string TargetPath { get; } = Path.GetFullPath(targetPath);
    
    public Uri SourceUri { get; } = uri;
    
    public Uri RealUri { get; private set; } = uri;
    
    public long ContentLength { get; private set; } = 0;
    
    public int ChunkSize { get; } = chunkSize;
    
    public int Retry { get; } = retry;

    public bool TrySegment { get; set; } = true;

    public LinkedList<DownloadSegment> Segments { get; } = [];
    
    public DownloadItemStatus Status { get; private set; } = DownloadItemStatus.Waiting;

    public event Action? Finished;

    public long CalculateTransferredLength()
    {
        lock (Segments) return Segments
            .Aggregate(0L, (len, task) => len + task.TransferredLength);
    }
    
    public long CalculateRemainingLength() => (ContentLength == 0) ? 0 : ContentLength - CalculateTransferredLength();
    
    private CancellationTokenSource _cancelTokenSource = new();

    public bool Cancel(bool markAsFailed = false)
    {
        if (Status is not DownloadItemStatus.Starting and DownloadItemStatus.Running) return false;
        _cancelTokenSource.Cancel();
        if (markAsFailed) Status = DownloadItemStatus.Failed;
        return true;
    }

    private int _finishedCount = 0;

    private void _ConstructDownloadSegment(
        long startPosition,
        long? endPosition,
        Action<DownloadSegment>? endCallback,
        CancellationToken cancelToken,
        int retry,
        out Task task,
        out DownloadSegment segment)
    {
        var seg = new DownloadSegment(RealUri, TargetPath, ChunkSize) { RetryCount = retry };
        if (endPosition is { } end) seg.EndPosition = end;
        if (startPosition == 0 && endPosition == 0)
        {
            seg.When(DownloadSegmentStatus.Running, () =>
            {
                RealUri = seg.RealUri;
                ContentLength = seg.TotalLength;
                Status = DownloadItemStatus.Running;
            });
        }
        seg.EndCallback += endCallback;
        segment = seg;
        task = seg.Start(startPosition != 0, startPosition, cancelToken);
    }

    public async Task NewSegment(
        long startPosition,
        long? endPosition,
        SegmentInterruptHandler errorCallback,
        LinkedListNode<DownloadSegment>? afterNode = null)
    {
        var cToken = _cancelTokenSource.Token;
        if (Segments.Count == 0)
        {
            // 初始化
            Status = DownloadItemStatus.Starting;
            cToken.Register(() => Status = DownloadItemStatus.Cancelled);
        }
        Task task;
        lock (Segments)
        {
            _ConstructDownloadSegment(
                startPosition, endPosition, (seg =>
                {
                    if (seg.Status == DownloadSegmentStatus.Success)
                    {
                        _finishedCount++;
                        if (_finishedCount != Segments.Count) return;
                        Status = DownloadItemStatus.Success;
                        Finished?.Invoke();
                    }
                    else if (seg.Status != DownloadSegmentStatus.Cancelled)
                    {
                        errorCallback(seg.Status, seg.LastException);
                    }
                }),
                cToken, Retry, out task, out var segment);
            if (afterNode != null)
            {
                Segments.AddAfter(afterNode, segment);
                afterNode.Value.EndPosition = startPosition - 1;
            }
            else Segments.AddLast(segment);
        }
        await task;
    }

    public async Task RestartSegment(
        LinkedListNode<DownloadSegment> node,
        bool isRetry = false)
    {
        Task task;
        lock (Segments)
        {
            var segment = node.Value;
            segment.Cancel();
            _ConstructDownloadSegment(
                segment.StartPosition, segment.EndPosition, segment.EndCallback,
                _cancelTokenSource.Token, segment.RetryCount, out task, out segment);
            node.Value = segment;
        }
        await task;
    }

    public override string ToString() => $"[{SourceUri}] -> [{TargetPath}]";
}
