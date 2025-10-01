using System;
using PCL.Core.UI.Animation.Core;

namespace PCL.Core.UI.Animation;

public class DoubleAnimation : BaseAnimation<double>
{
    public override IAnimationFrame ComputeNextFrame()
    {
        var progress = (double)CurrentFrame / TotalFrames;
        
        // 应用缓动函数
        var easedProgress = Info!.Easing.Ease(progress);
        
        // 计算当前值
        CurrentValue = Info.From + Info.To * easedProgress;

        return base.ComputeNextFrame();
    }
}