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
    private bool _isResetting = false;

    /// <summary>
    /// 重置延时。
    /// </summary>
    public async Task Reset()
    {
        // 上锁
        lock (_resetLock)
        {
            if (_isResetting) return;
            _isResetting = true;
        }
        IsCurrentTaskCompleted = false;
        // 结束当前延迟
        if (_currentDelayCts != null)
        {
            try { await _currentDelayCts.CancelAsync().ConfigureAwait(false); }
            catch (Exception) { /* ignored */ }
        }
        // 等待当前任务完成
        if (_currentTask != null)
        {
            await _currentTask.ConfigureAwait(false);
            _currentTask = null;
        }
        // 开始新延迟
        _currentDelayCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
#pragma warning disable CS4014 // 禁用编译器的智障提示
        Task.Run(async () =>
#pragma warning restore CS4014 // 谁规定 async 里面必须 await 了
        {
            var cToken = _currentDelayCts.Token;
            try { await Task.Delay(Delay, cToken).ConfigureAwait(false); }
            catch (Exception) { /* ignored */ }
            if (!cToken.IsCancellationRequested)
            {
                _currentTask = ScheduledTask();
                await _currentTask.ConfigureAwait(false);
            }
            _currentTask = null;
        }, _cts.Token);
        // 解锁
        lock (_resetLock) _isResetting = false;
    }

    public void Dispose()
    {
        try { _cts.Cancel(); }
        catch (Exception) { /* ignored */ }
        _currentDelayCts?.Dispose();
        _cts.Dispose();
        _currentTask?.Dispose();
        GC.SuppressFinalize(this);
    }
}
