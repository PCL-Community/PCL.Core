using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace PCL.Core.UI.Animation.Core;

public class AnimationExtensions
{
    public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target", typeof(DependencyObject), typeof(AnimationExtensions), new PropertyMetadata(default(DependencyObject)));

    public static void SetTarget(DependencyObject element, DependencyObject value)
    {
        if (element is not IAnimation)
            throw new InvalidOperationException("AnimationExtensions.Target 只能附加到 IAnimation 实例上。");
        
        element.SetValue(TargetProperty, value);
    }

    public static DependencyObject GetTarget(DependencyObject element)
    {
        return (DependencyObject)element.GetValue(TargetProperty);
    }

    public static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached(
        "TargetProperty", typeof(DependencyProperty), typeof(AnimationExtensions), new PropertyMetadata(default(DependencyProperty)));

    [TypeConverter(typeof(DependencyPropertyConverter))]
    public static void SetTargetProperty(DependencyObject element, DependencyProperty value)
    {
        if (element is not IAnimation)
            throw new InvalidOperationException("AnimationExtensions.TargetProperty 只能附加到 IAnimation 实例上。");
        
        element.SetValue(TargetPropertyProperty, value);
    }

    public static DependencyProperty GetTargetProperty(DependencyObject element)
    {
        return (DependencyProperty)element.GetValue(TargetPropertyProperty);
    }
}