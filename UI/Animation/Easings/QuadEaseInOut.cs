﻿namespace PCL.Core.UI.Animation.Easings;

public class QuadEaseInOut : Easing
{
    protected override double EaseCore(double progress)
    {
        if (progress < 0.5)
        {
            return 2 * progress * progress;
        }

        return progress * (4 - 2 * progress) - 1;
    }
}