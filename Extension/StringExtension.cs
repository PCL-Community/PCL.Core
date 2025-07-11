using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace PCL.Core.Extension;

public static class StringExtension
{
    public static object? Convert(string? value, Type targetType)
    {
        if (targetType is null)
            throw new ArgumentNullException(nameof(targetType));

        // 1) 目标就是 string，本身即允许为 null
        if (targetType == typeof(string))
            return value;

        // 2) 输入为 null
        if (value is null)
        {
            // 引用类型或 Nullable<T> → 返回 null
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                return null;
            // 非 Nullable 值类型 → default(T)
            return Activator.CreateInstance(targetType);
        }

        // 3) TypeConverter.GetConverter(Type) → 返回非 null 的 TypeConverter
        TypeConverter converter = TypeDescriptor.GetConverter(targetType);

        //    CanConvertFrom(Type) → bool
        if (converter.CanConvertFrom(typeof(string)))
        {
            // ConvertFromString(ITypeDescriptorContext? context, CultureInfo? culture, string text)
            // — context、culture 可传 null，text 为非 null；返回 object?（具体 Converter 可返回 null）
            var c = converter.ConvertFromInvariantString(value);
            return c;
        }

        // 4) IConvertible 情形  
        if (typeof(IConvertible).IsAssignableFrom(targetType))
        {
            // ChangeType(object value, Type conversionType, IFormatProvider? provider)
            // — value: object?；provider 可为 null；返回 [NotNullIfNotNull("value")] object?
            //   由于此处 value != null，文档保证返回非 null
            object changed = System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)!;
            return changed;
        }

        // 5) Enum.Parse(Type enumType, string value, bool ignoreCase)
        // — value: non-null；返回非 null boxed enum
        if (targetType.IsEnum)
        {
            return Enum.Parse(targetType, value, ignoreCase: true);
        }

        // 6) 静态 Parse(string) 方法
        var parse = targetType.GetMethod(
            "Parse",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(string)],
            modifiers: null);
        if (parse is not null)
        {
            // MethodInfo.Invoke(object? obj, object?[]? parameters)
            // — 返回 object?（由被调用方法决定是否可能为 null）
            return parse.Invoke(null, [value]);
        }

        throw new NotSupportedException($"无法将字符串转换为类型 {targetType.FullName}");
    }
    
    public static T? Convert<T>(this string? value)
    {
        var obj = Convert(value, typeof(T));
        if (obj is null) return default;
        return (T)obj;
    }

    public static string? ConvertToString(object? obj)
    {
        if (obj == null) return null;
        if (obj is string s) return s;

        // GetConverter(Type) → 非 null
        TypeConverter converter = TypeDescriptor.GetConverter(obj.GetType());
        if (converter.CanConvertTo(typeof(string)))
        {
            // ConvertToString(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            // — 返回 object?（具体 Converter 决定是否可为 null）
            object? o = converter.ConvertToInvariantString(obj);
            return o as string;
        }

        if (obj is IFormattable fmt)
        {
            // IFormattable.ToString(string? format, IFormatProvider? provider)
            // — 返回 string?（一般非 null，但签名允许 null）
            return fmt.ToString(null, CultureInfo.InvariantCulture);
        }

        // object.ToString(): 返回非 null string
        return obj.ToString();
    }

    public static string? ConvertToString<T>(this T? value)
        => ConvertToString((object?)value);
}
