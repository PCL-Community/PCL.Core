using System;
using System.Threading.Tasks;
using PCL.Core.Utils.Threading;

namespace PCL.Core.App.Configuration.NTraffic;

/// <summary>
/// 异步物流中心，提供可选异步执行符合条件操作的消费实现。
/// </summary>
public abstract class AsyncTrafficCenter : TrafficCenter
{
    private readonly LimitedTaskPool _taskPool = new(1);

    protected sealed override void OnTraffic<TInput, TOutput>(
        PreviewTrafficEventArgs<TInput, TOutput> e,
        Action<PreviewTrafficEventArgs<TInput, TOutput>> onInvokeEvent)
    {
        if (CanAsync(e)) _taskPool.Submit(async () =>
        {
            await OnTrafficAsync(e).ConfigureAwait(false);
            onInvokeEvent(e);
        });
        else
        {
            OnTrafficSync(e);
            onInvokeEvent(e);
        }
    }

    protected virtual void OnTrafficSync<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e) { }

    protected virtual Task OnTrafficAsync<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e) => Task.CompletedTask;

    protected virtual bool CanAsync<TInput, TOutput>(TrafficEventArgs<TInput, TOutput> e) => true;
}
