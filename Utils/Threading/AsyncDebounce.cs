using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

/// <summary>
/// 可重置的异步延时器。在指定延时后执行异步任务并等待下一次重置后重复该逻辑，指定延时未到达时重置将会重新开始计时。
/// <p>实例创建后并不会立即开始计时，而是等待第一次 <see cref="Reset"/>
/// 调用。因此，若有特殊需求，请不要忘了创建实例后调用一次 <see cref="Reset"/>。</p>
/// </summary>
public class AsyncDebounce(CancellationToken cancelToken = default) : IDisposable
{
    /// <summary>
    /// 执行延迟。
    /// </summary>
    public required TimeSpan Delay { get; init; }

    /// <summary>
    /// 异步任务实例。
    /// </summary>
    public required Func<Task> ScheduledTask { get; init; }

    /// <summary>
    /// 指示本次延迟任务是否已经完成。
    /// </summary>
    public bool IsCurrentTaskCompleted { get; private set; }

    private Task? _currentTask;
    private CancellationTokenSource? _currentDelayCts;
    private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
    private readonly object _resetLock = new();

    /// <summary>
    /// 重置延时。
    /// </summary>
    public async Task Reset()
    {
        IsCurrentTaskCompleted = false;
        lock (_resetLock)
        {
            // 取消并丢弃旧的 CTS
            _currentDelayCts?.Cancel();
            _currentDelayCts?.Dispose();
            _currentDelayCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

            // 捕获当前 CTS
            var capturedCts = _currentDelayCts;
            _ = Task.Run(async () =>
            {
                try { await Task.Delay(Delay, capturedCts.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }

                // 身份校验: 只有仍然是当前 CTS 的 worker 才能继续
                if (
                    !ReferenceEquals(_currentDelayCts, capturedCts) ||
                    capturedCts.IsCancellationRequested ||
                    _cts.IsCancellationRequested
                ) return;

                // 执行 ScheduledTask
                var task = ScheduledTask();
                lock (_resetLock) _currentTask = task;
                try
                {
                    await task.ConfigureAwait(false);
                }
                finally
                {
                    lock (_resetLock)
                    {
                        _currentTask = null;
                        IsCurrentTaskCompleted = true;
                    }
                }
            }, _cts.Token);
        }

        // 等待当前任务，确保 ScheduledTask 不重叠执行
        Task? running;
        lock (_resetLock) running = _currentTask;
        if (running != null) await running.ConfigureAwait(false);
    }

    public void Dispose()
    {
        try { _cts.Cancel(); }
        catch (Exception) { /* ignored */ }
        _currentDelayCts?.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
