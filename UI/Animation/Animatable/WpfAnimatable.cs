using System;
using System.Windows;
using PCL.Core.UI.Animation.ValueFilter;

namespace PCL.Core.UI.Animation.Animatable;

public sealed class WpfAnimatable(DependencyObject owner, DependencyProperty? property) : IAnimatable
{
    public DependencyObject Owner { get; set; } = owner;
    public DependencyProperty? Property { get; set; } = property;

    public object? GetValue()
    {
        DependencyProperty? actualProperty;

        if (Property == FrameworkElement.WidthProperty)
        {
            actualProperty = FrameworkElement.ActualWidthProperty;
        }
        else if (Property == FrameworkElement.HeightProperty)
        {
            actualProperty = FrameworkElement.ActualHeightProperty;
        }
        else
        {
            actualProperty = Property;
        }

        ArgumentNullException.ThrowIfNull(actualProperty);
        
        return Owner.GetValue(actualProperty);
    }

    public void SetValue(object value)
    {
        value = ValueFilterManager.Apply(value);
        ArgumentNullException.ThrowIfNull(Property);
        Owner.SetValue(Property, value);
    }
}