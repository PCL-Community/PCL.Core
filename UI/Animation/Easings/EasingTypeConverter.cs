using System;
using System.ComponentModel;
using System.Globalization;

namespace PCL.Core.UI.Animation.Easings;

public class EasingTypeConverter : TypeConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
        {
            return s switch
            {
                "BackEaseIn" => new BackEaseIn(),
                "BackEaseOut" => new BackEaseOut(),
                "BackEaseInOut" => new BackEaseInOut(),
                "BounceEaseIn" => new BounceEaseIn(),
                "BounceEaseOut" => new BounceEaseOut(),
                "BounceEaseInOut" => new BounceEaseInOut(),
                "CircularEaseIn" => new CircularEaseIn(),
                "CircularEaseOut" => new CircularEaseOut(),
                "CircularEaseInOut" => new CircularEaseInOut(),
                "CubicEaseIn" => new CubicEaseIn(),
                "CubicEaseOut" => new CubicEaseOut(),
                "CubicEaseInOut" => new CubicEaseInOut(),
                "ElasticEaseIn" => new ElasticEaseIn(),
                "ElasticEaseOut" => new ElasticEaseOut(),
                "ElasticEaseInOut" => new ElasticEaseInOut(),
                "ExponentialEaseIn" => new ExponentialEaseIn(),
                "ExponentialEaseOut" => new ExponentialEaseOut(),
                "ExponentialEaseInOut" => new ExponentialEaseInOut(),
                "LinearEasing" => new LinearEasing(),
                "QuadEaseIn" => new QuadEaseIn(),
                "QuadEaseOut" => new QuadEaseOut(),
                "QuadEaseInOut" => new QuadEaseInOut(),
                "QuarticEaseIn" => new QuarticEaseIn(),
                "QuarticEaseOut" => new QuarticEaseOut(),
                "QuarticEaseInOut" => new QuarticEaseInOut(),
                "QuinticEaseIn" => new QuinticEaseIn(),
                "QuinticEaseOut" => new QuinticEaseOut(),
                "QuinticEaseInOut" => new QuinticEaseInOut(),
                "SineEaseIn" => new SineEaseIn(),
                "SineEaseOut" => new SineEaseOut(),
                "SineEaseInOut" => new SineEaseInOut(),
                _ => throw new NotSupportedException($"不支持的缓动: {s}")
            };
        }
        return base.ConvertFrom(context, culture, value);
    }
}