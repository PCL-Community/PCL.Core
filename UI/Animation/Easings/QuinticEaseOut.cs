﻿namespace PCL.Core.UI.Animation.Easings;

public class QuinticEaseOut : Easing
{
    protected override double EaseCore(double progress)
    {
        var f = progress - 1d;
        var f2 = f * f;
        return f2 * f2 * f + 1d;
    }
}