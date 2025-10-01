using System.Windows;

namespace PCL.Core.UI.Animation.Animatable;

public class WpfAnimatable(DependencyObject owner, DependencyProperty property) : IAnimatable
{
    public DependencyObject Owner { get; set; } = owner;
    public DependencyProperty Property { get; set; } = property;

    public object? GetValue()
    {
        DependencyProperty property;

        if (Property == FrameworkElement.WidthProperty)
        {
            property = FrameworkElement.ActualWidthProperty;
        }
        else if (Property == FrameworkElement.HeightProperty)
        {
            property = FrameworkElement.ActualHeightProperty;
        }
        else
        {
            property = Property;
        }

        return Owner.GetValue(property);
    }

    public void SetValue(object value)
    {
        Owner.SetValue(Property, value);
    }
}