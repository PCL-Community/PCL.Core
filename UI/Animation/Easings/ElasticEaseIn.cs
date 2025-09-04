using System;

namespace PCL.Core.UI.Animation.Easings;

public class ElasticEaseIn : Easing
{
    protected override double EaseCore(double progress)
    {
        return Math.Sin(13 * (Math.PI / 2) * progress) * Math.Pow(2, 10 * (progress - 1));  
    }
}