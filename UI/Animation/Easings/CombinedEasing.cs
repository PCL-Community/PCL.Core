using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.UI.Animation.Easings;

/// <summary>
/// 复合缓动，可以叠加多个缓动，每个缓动有自己的持续时间。
/// </summary>
public class CompositeEasing : Easing
{
    private readonly List<(IEasing easing, TimeSpan duration)> _easings;
    private readonly TimeSpan _totalDuration;

    public CompositeEasing(params (IEasing easing, TimeSpan duration)[] easings)
    {
        if (easings == null || easings.Length == 0)
            throw new ArgumentException("至少需要一个缓动");

        _easings = new List<(IEasing easing, TimeSpan duration)>(easings);
        _totalDuration = easings.Max(e => e.duration);
    }

    /// <summary>
    /// 总时长
    /// </summary>
    public TimeSpan TotalDuration => _totalDuration;

    protected override double EaseCore(double progress)
    {
        var elapsed = _totalDuration * progress;

        var value = 0.0;
        var totalWeight = 0.0;

        foreach (var (easing, duration) in _easings)
        {
            var elapsedForEasing = elapsed < duration ? elapsed : duration;
            if (elapsedForEasing <= TimeSpan.Zero)
                continue;

            var localProgress = elapsedForEasing.TotalSeconds / duration.TotalSeconds;
            var easingValue = easing.Ease(localProgress);

            value += easingValue;
            totalWeight += 1.0;
        }

        if (totalWeight > 0)
            value /= totalWeight;

        return value;
    }
}