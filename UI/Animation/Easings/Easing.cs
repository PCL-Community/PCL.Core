namespace PCL.Core.UI.Animation.Easings;

/// <summary>
/// 所有缓动类的基类。
/// </summary>
public abstract class Easing : IEasing
{
    protected abstract double EaseCore(double progress);

    public double Ease(double progress)
    {
        if (progress <= 0.0) return 0.0;
        if (progress >= 1.0) return 1.0;
        return EaseCore(progress);
    }

    /// <summary>
    /// 返回指定动画帧的过渡值。
    /// </summary>
    /// <param name="currentFrame">当前动画帧。</param>
    /// <param name="totalFrames">总动画帧数量。</param>
    /// <returns>过渡值。</returns>
    public double Ease(int currentFrame, int totalFrames)
    {
        if (totalFrames <= 0) return 0.0;
        return Ease((double)currentFrame / totalFrames);
    }
}