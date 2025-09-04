using System;

namespace PCL.Core.UI.Animation.Easings;

public class ElasticEaseInOut : Easing
{
    protected override double EaseCore(double progress)
    {
        if (progress < 0.5d)
        {
            var t = 2 * progress;
            return 0.5 * Math.Sin(13 * (Math.PI / 2) * t) * Math.Pow(2d, 10d * (t - 1));
        }

        return 0.5 * (Math.Sin(-13 * (Math.PI / 2) * (2 * progress - 1 + 1)) * Math.Pow(2, -10 * (2 * progress - 1)) + 2);
    }
}