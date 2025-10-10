using System;
using System.Collections.Generic;

namespace PCL.Core.UI.Animation.ValueFilter;

public static class ValueFilterManager
{
    private static readonly Dictionary<Type, Func<object, object>> _Filters = new();
    
    public static void Register<T>(IValueFilter<T> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // 直接生成一个闭包委托，用于类型转换
        _Filters[typeof(T)] = o => filter.Filter((T)o)!;
    }
    
    public static object Apply(object value)
    {
        var t = value.GetType();
        
        // 没有匹配时 passthrough
        return _Filters.TryGetValue(t, out var func) ? func(value) : value;
    }
}