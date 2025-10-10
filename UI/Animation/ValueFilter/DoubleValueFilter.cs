using System;

namespace PCL.Core.UI.Animation.ValueFilter;

public class DoubleValueFilter : IValueFilter<double>
{
    public double Filter(double value)
    {
        return Math.Max(0, value);
    }
}