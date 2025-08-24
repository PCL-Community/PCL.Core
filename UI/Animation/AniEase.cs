using System;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation;

// 基类
public enum AniEasePower : int
{
    Weak = 2,
    Middle = 3,
    Strong = 4,
    ExtraStrong = 5
}

/// <summary>
/// 缓动函数基类。
/// </summary>
public abstract class AniEase
{
    /// <summary>
    /// 获取函数值。
    /// </summary>
    /// <param name="t">时间百分比。</param>
    public abstract double GetValue(double t);
    /// <summary>
    /// 获取增量值。
    /// </summary>
    /// <param name="t1">较大的 X。</param>
    /// <param name="t0">较小的 X。</param>
    public virtual double GetDelta(double t1, double t0)
        => GetValue(t1) - GetValue(t0);
}

/// <summary>
/// 渐入渐出组合。
/// </summary>
public class AniEaseInout(AniEase easeIn, AniEase easeOut, double easeInPercent = 0.5d) : AniEase
{
    private readonly AniEase _easeIn = easeIn;
    private readonly AniEase _easeOut = easeOut;
    private readonly double _easeInPercent = easeInPercent;

    public override double GetValue(double t)
    {
        if (t < _easeInPercent)
            return _easeInPercent * _easeIn.GetValue(t / _easeInPercent);
        return (1d - _easeInPercent) * _easeOut.GetValue((t - _easeInPercent) / (1d - _easeInPercent)) + _easeInPercent;
    }
}

// Linear / 线性
/// <summary>
/// 线性，无缓动。
/// </summary>
public class AniEaseLinear : AniEase
{
    public override double GetValue(double t)
        => MathUtils.Clamp(t, 0d, 1d);
    public override double GetDelta(double t1, double t0)
        => MathUtils.Clamp(t1, 0d, 1d) - MathUtils.Clamp(t0, 0d, 1d);
}

// Fluent / 平滑
/// <summary>
/// 平滑开始。
/// </summary>
public class AniEaseInFluent(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly AniEasePower _p = power;

    public override double GetValue(double t)
        => Math.Pow(MathUtils.Clamp(t, 0d, 1d), (double)_p);
}
/// <summary>
/// 平滑结束。
/// </summary>
public class AniEaseOutFluent(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly AniEasePower _p = power;

    public override double GetValue(double t)
        => 1d - Math.Pow(MathUtils.Clamp(1d - t, 0d, 1d), (double)_p);
}
/// <summary>
/// 平滑开始与结束。
/// </summary>
public class AniEaseInoutFluent(AniEasePower power = AniEasePower.Middle, double middle = 0.5d) : AniEase
{
    private AniEaseInout _ease = new(new AniEaseInFluent(power), new AniEaseOutFluent(power), middle);

    public override double GetValue(double t) => _ease.GetValue(t);
}
/// <summary>
/// 以特定速度开始的平滑结束。
/// </summary>
public class AniEaseOutFluentWithInitial : AniEase
{
    private readonly double _alpha; // (初速度 / 平均速度) – 1

    /// <param name="initialPixelPerSecond">初速度，px/s</param>
    /// <param name="totalSecond">总时长，s</param>
    /// <param name="totalDistance">总路程，px</param>
    public AniEaseOutFluentWithInitial(double initialPixelPerSecond, double totalSecond, double totalDistance)
    {
        double v0_norm = initialPixelPerSecond * totalSecond / totalDistance; // 归一化初速度
        _alpha = v0_norm - 1.0d;
        if (_alpha < 0d)
            _alpha = 0d; // 初速度小于平均速度时，退化为线性
    }
    public override double GetValue(double percent)
    {
        double p = MathUtils.Clamp(percent, 0d, 1d);
        if (_alpha == 0d)
            return p; // 退化到线性
        return (_alpha + 1d) * p / (1d + _alpha * p);
    }
}

// Back / 回弹
/// <summary>
/// 回弹开始。有效时间为 1/3。
/// </summary>
public class AniEaseInBack(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly double _p = 3d - (double)power * 0.5d;

    public override double GetValue(double t)
    {
        t = MathUtils.Clamp(t, 0d, 1d);
        return Math.Pow(t, _p) * Math.Cos(1.5d * Math.PI * (1d - t));
    }
}
/// <summary>
/// 回弹结束。有效时间为 1/3。
/// </summary>
public class AniEaseOutBack(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly double _p = 3d - (double)power * 0.5d;

    public override double GetValue(double t)
    {
        t = MathUtils.Clamp(t, 0d, 1d);
        return 1d - Math.Pow(1d - t, _p) * Math.Cos(1.5d * Math.PI * t);
    }
}

// Car / 平滑-回弹
/// <summary>
/// 回弹开始，短平滑结束。
/// </summary>
public class AniEaseInCar(double middle = 0.7d, AniEasePower power = AniEasePower.Middle) : AniEase
{
    private AniEaseInout _ease = new(new AniEaseInBack(power), new AniEaseOutFluent(power), middle);

    public override double GetValue(double t)
        => _ease.GetValue(t);
}
/// <summary>
/// 短平滑开始，回弹结束。
/// </summary>
public class AniEaseOutCar(double middle = 0.3d, AniEasePower power = AniEasePower.Middle) : AniEase
{
    private AniEaseInout _ease = new AniEaseInout(new AniEaseInFluent(power), new AniEaseOutBack(power), middle);

    public override double GetValue(double t)
        => _ease.GetValue(t);
}

// Elastic / 弹簧
/// <summary>
/// 弹簧开始。约在 60% 到达最小值。
/// </summary>
public class AniEaseInElastic(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly int _p = (int)power + 4; // 6~9

    public override double GetValue(double t)
    {
        t = MathUtils.Clamp(t, 0d, 1d);
        return Math.Pow(t, (_p - 1) * 0.25d) * Math.Cos((_p - 3.5d) * Math.PI * Math.Pow(1d - t, 1.5d));
    }
}
/// <summary>
/// 弹簧结束。约在 40% 到达最大值。
/// </summary>
public class AniEaseOutElastic(AniEasePower power = AniEasePower.Middle) : AniEase
{
    private readonly int _p = (int)power + 4;

    public override double GetValue(double t)
    {
        t = 1d - MathUtils.Clamp(t, 0d, 1d);
        return 1d - Math.Pow(t, (_p - 1) * 0.25d) * Math.Cos((_p - 3.5d) * Math.PI * Math.Pow(1d - t, 1.5d));
    }
}