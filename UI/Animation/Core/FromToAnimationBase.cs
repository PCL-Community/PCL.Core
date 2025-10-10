using System;
using System.Numerics;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Animation.Core;

public class FromToAnimationBase<T> : AnimationBase, IFromToAnimation
    where T : IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
{
    public IEasing Easing { get; set; } = new LinearEasing();
    public T? From { get; set; }
    public T To { get; set; } = default!;
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

    /// <summary>
    /// 运行动画。
    /// </summary>
    /// <exception cref="ArgumentNullException">当动画运行必须的值为空时出现。</exception>
    public override async Task RunAsync(IAnimatable target)
    {
        // 重置当前帧
        CurrentFrame = 0;
        
        // 空值检查
        ArgumentNullException.ThrowIfNull(To);
        
        // 记录初始值
        _startValue = (T)target.GetValue()!;
        
        // 如果 From 为空，则根据动画值类型设置初始值
        From ??= ValueType == AnimationValueType.Relative ? default : _startValue;
        
        // 计算总帧数
        TotalFrames = (int)Math.Round(Duration.TotalSeconds * AnimationService.Fps / AnimationService.Scale);
        
        // 进行初始赋值
        target.SetValue(ValueType == AnimationValueType.Relative ? From! + _startValue! : From!);
        
        // 延迟
        await Task.Delay(Delay);
        
        // 将该动画推送到动画服务
        await AnimationService.PushAnimationAsync((FromToAnimationBase<T>)MemberwiseClone(), target);
    }

    public override void Cancel()
    {
        // 重置值
        CurrentFrame = 0;
        TotalFrames = 0;
        
        
    }

    public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
    {
        return new AnimationFrame<T>
        {
            Target = target,
            Value = ValueType == AnimationValueType.Relative ? CurrentValue! : CurrentValue! - From!,
            StartValue = ValueType == AnimationValueType.Relative ? _startValue! : From!
        };
    }
}