using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

/// <summary>
/// 用于在动画系统中执行 Action。
/// </summary>
public class ActionAnimation : AnimationBase
{
    public ActionAnimation() { }

    public ActionAnimation(Action action) => Action = _ => action();

    public ActionAnimation(Action<CancellationToken> action) => Action = action;

    public Action<CancellationToken> Action { get; set; } = null!;
    public override int CurrentFrame { get; set; }
    public TimeSpan Delay { get; set; }
    
    private CancellationTokenSource? _cts = new();
    private TaskCompletionSource? _tcs;
    
    
    public override async Task<IAnimation> RunAsync(IAnimatable target)
    {
        ArgumentNullException.ThrowIfNull(Action);
        
        _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cts = new CancellationTokenSource();
        
        var clone = (ActionAnimation)MemberwiseClone();
        clone.Status = AnimationStatus.Running;
        
        // 延迟
        await Task.Delay(Delay);
        
        _ = AnimationService.PushAnimationAsync(clone, target);
        return await _tcs.Task.ContinueWith<IAnimation>(_ => clone);
    }

    public override IAnimation RunFireAndForget(IAnimatable target)
    {
        ArgumentNullException.ThrowIfNull(Action);

        _cts = new CancellationTokenSource();
        
        var clone = (ActionAnimation)MemberwiseClone();
        clone.Status = AnimationStatus.Running;
        
        _ = Task.Run(async () =>
        {
            // 延迟
            await Task.Delay(Delay);
            
            AnimationService.PushAnimationFireAndForget(clone, target);
        });
        
        return clone;
    }

    public override void Cancel()
    {
        Status = AnimationStatus.Canceled;
        _cts?.Cancel();
        _tcs?.TrySetCanceled();
    }

    public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
    {
        if (Status is AnimationStatus.Canceled or AnimationStatus.Completed) return null;
        
        return new ActionAnimationFrame(() =>
        {
            Action(_cts!.Token);
            Status = AnimationStatus.Completed;
            _tcs?.TrySetResult();
        });
    }
}