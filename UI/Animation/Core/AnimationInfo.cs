using System;
using PCL.Core.UI.Animation.Easings;

namespace PCL.Core.UI.Animation.Core;

public record AnimationInfo<T>
{
    public IEasing Easing { get; set; } = new LinearEasing();
    public T? From { get; set; }
    public T To { get; set; } = default!;
    public AnimationValueType ValueType { get; set; } = AnimationValueType.Relative;
    public TimeSpan Duration { get; set; }
    public TimeSpan Delay { get; set; }

    #region 链式调用
    
    public AnimationInfo<T> SetEasing(IEasing easing)
    {
        Easing = easing ?? throw new ArgumentNullException(nameof(easing));
        return this;
    }
    
    public AnimationInfo<T> SetFrom(T from)
    {
        From = from;
        return this;
    }
    
    public AnimationInfo<T> SetTo(T to)
    {
        To = to;
        return this;
    }
    
    public AnimationInfo<T> SetValueType(AnimationValueType valueType)
    {
        ValueType = valueType;
        return this;
    }
    
    public AnimationInfo<T> SetDuration(TimeSpan duration)
    {
        Duration = duration;
        return this;
    }
    
    public AnimationInfo<T> SetDelay(TimeSpan delay)
    {
        Delay = delay;
        return this;
    }
    
    #endregion
}