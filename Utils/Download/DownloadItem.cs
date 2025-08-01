using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PCL.Core.Utils.Download;

/// <summary>
/// 并行任务中断处理。
/// </summary>
/// <param name="status">任务状态</param>
/// <param name="lastException">任务中断前抛出的最后一个异常</param>
public delegate void ParallelInterruptHandler(ParallelTaskStatus status, Exception? lastException);

/// <summary>
/// 下载项状态。
/// </summary>
public enum DownloadItemStatus
{
    Success,
    Waiting,
    Running,
    Cancelled,
    Failed,
}

/// <summary>
/// 下载项。
/// </summary>
public class DownloadItem(Uri uri, string targetPath, int chunkSize, int retry)
{
    public string TargetPath { get; } = Path.GetFullPath(targetPath);
    
    public Uri SourceUri { get; } = uri;
    
    public Uri RealUri { get; private set; } = uri;
    
    public long ContentLength { get; private set; } = 0;
    
    public int ChunkSize { get; } = chunkSize;
    
    public int Retry { get; } = retry;

    public LinkedList<ParallelTask> Parallels { get; } = [];
    
    public DownloadItemStatus Status { get; private set; } = DownloadItemStatus.Waiting;

    public long CalculateTransferredLength()
    {
        lock (Parallels)
        {
            return Parallels.Aggregate(0L, (len, task) => len + task.TransferredLength);
        }
    }
    
    private int _finishedCount = 0;

    public void Insert(
        long startPosition,
        long endPosition,
        Action finishedCallback,
        ParallelInterruptHandler errorCallback,
        LinkedListNode<ParallelTask>? afterNode = null)
    {
        lock (Parallels)
        {
            var task = new ParallelTask(RealUri, TargetPath, ChunkSize)
            {
                EndPosition = endPosition,
                RetryCount = Retry,
            };
            if (startPosition == 0 && endPosition != 0)
            {
                task.When(ParallelTaskStatus.Running, () =>
                {
                    RealUri = task.RealUri;
                    ContentLength = task.TotalLength;
                    Status = DownloadItemStatus.Running;
                });
            }
            task.EndCallback += (() =>
            {
                if (task.Status == ParallelTaskStatus.Success)
                {
                    _finishedCount++;
                    if (_finishedCount != Parallels.Count) return;
                    Status = DownloadItemStatus.Success;
                    finishedCallback();
                }
                else errorCallback(task.Status, task.LastException);
            });
            task.Start(startPosition != 0, startPosition);
            if (afterNode != null) Parallels.AddAfter(afterNode, task);
            else Parallels.AddLast(task);
        }
    }
}
