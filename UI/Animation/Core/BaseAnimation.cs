using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public abstract class BaseAnimation<T> : IAnimation where T : IAdditionOperators<T, T, T>
{
    public IAnimatable? Target { get; set; }
    public AnimationInfo<T>? Info { get; set; }
    public T? CurrentValue { get; internal set; }
    object? IAnimation.CurrentValue
    {
        get => CurrentValue;
        set
        {
            if (value is not T typed) throw new InvalidCastException($"无法将 {value!.GetType()} 转换为 {typeof(T)}");
            CurrentValue = typed;
        }
    }

    public int CurrentFrame { get; set; } = 0;
    public int TotalFrames { get; set; }
    
    public bool IsCompleted => CurrentFrame >= TotalFrames;
    
    private T? _startValue;

    /// <summary>
    /// 运行动画。
    /// </summary>
    /// <exception cref="ArgumentNullException">当动画运行必须的值为空时出现。</exception>
    public virtual async Task RunAsync()
    {
        // 空值检查
        ArgumentNullException.ThrowIfNull(Target);
        ArgumentNullException.ThrowIfNull(Info);
        ArgumentNullException.ThrowIfNull(Info.To);
        
        // 记录初始值
        _startValue = (T)Target.GetValue()!;
        
        // 如果 From 为空，则根据动画值类型设置初始值
        Info.From ??= Info.ValueType == AnimationValueType.Relative ? default : _startValue;
        
        // 计算总帧数
        TotalFrames = (int)Math.Round(Info.Duration.TotalSeconds * AnimationService.Fps / AnimationService.Scale);
        
        // 进行初始赋值
        Target.SetValue(Info.ValueType == AnimationValueType.Relative ? Info.From! + _startValue! : Info.From!);
        
        // 将该动画推送到动画服务
        await AnimationService.PushAnimationAsync(this);
    }

    public void Cancel()
    {
        // 重置值
        CurrentFrame = 0;
        TotalFrames = 0;
    }

    public virtual IAnimationFrame ComputeNextFrame()
    {
        return new AnimationFrame<T>
        {
            Target = Target!,
            Value = CurrentValue!,
            StartValue = Info!.ValueType == AnimationValueType.Relative ? _startValue! : Info.From!
        };
    }
}