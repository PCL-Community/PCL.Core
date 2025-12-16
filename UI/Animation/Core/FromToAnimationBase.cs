using System;
using System.Numerics;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Easings;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation.Core;

public class FromToAnimationBase<T> : AnimationBase, IFromToAnimation
{
    public IEasing Easing { get; set; } = new LinearEasing();
    private bool _hasFromValue;
    private T _from = default!;
    public T From
    {
        get => _from;
        set
        {
            _hasFromValue = true;
            _from = value;
        }
    }
    public T? To { get; set; }
    public AnimationValueType ValueType { get; set; } = AnimationValueType.Relative;
    public TimeSpan Duration { get; set; }
    public TimeSpan Delay { get; set; }
    public T? CurrentValue { get; internal set; }

    object? IFromToAnimation.CurrentValue
    {
        get => CurrentValue;
        set
        {
            if (value is not T typed) throw new InvalidCastException($"无法将 {value!.GetType()} 转换为 {typeof(T)}");
            CurrentValue = typed;
        }
    }

    public int TotalFrames { get; private set; }

    public override bool IsCompleted => CurrentFrame >= TotalFrames;
    public override int CurrentFrame { get; set; }
    
    private T? _startValue;

    public override async Task<IAnimation> RunAsync(IAnimatable target)
    {
        _RunCore(target);
        var clone = (FromToAnimationBase<T>)MemberwiseClone();

        // 延迟
        await Task.Delay(Delay);

        // 将该动画推送到动画服务
        await AnimationService.PushAnimationAsync(clone, target);

        return clone;
    }

    public override IAnimation RunFireAndForget(IAnimatable target)
    {
        _RunCore(target);
        var clone = (FromToAnimationBase<T>)MemberwiseClone();

        _ = Task.Run(async () =>
        {
            // 延迟
            await Task.Delay(Delay);

            // 将该动画推送到动画服务
            AnimationService.PushAnimationFireAndForget(clone, target);
        });
        
        return clone;
    }

    private void _RunCore(IAnimatable target)
    {
        // 重置当前帧
        CurrentFrame = 0;

        // 空值检查
        ArgumentNullException.ThrowIfNull(To);

        // 记录初始值
        _startValue = (T)target.GetValue()!;

        // 如果 From 为空，则根据动画值类型设置初始值
        if (!_hasFromValue)
        {
           From = ValueType == AnimationValueType.Relative ? ValueProcessorManager.DefaultValue<T>() : _startValue;
        }

        // 计算总帧数
        TotalFrames = (int)Math.Round(Duration.TotalSeconds * AnimationService.Fps / AnimationService.Scale);

        // 进行初始赋值
        // target.SetValue(
        //     ValueType == AnimationValueType.Relative ? ValueProcessorManager.Add(From, _startValue)! : From!);
    }

    public override void Cancel()
    {
        // 确保正常结束
        CurrentFrame = TotalFrames + 1;
    }

    public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
    {
        return new AnimationFrame<T>
        {
            Target = target,
            Value = ValueType == AnimationValueType.Relative
                ? CurrentValue!
                : ValueProcessorManager.Subtract(CurrentValue!, From!),
            StartValue = ValueType == AnimationValueType.Relative ? _startValue! : From!
        };
    }
}