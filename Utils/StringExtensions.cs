using System;

namespace PCL.Core.Utils;

public static class StringExtensions
{
    /// <summary>
    /// 获取在指定分隔符第一次出现之前的部分，返回一个零分配的视图。<br/>
    /// 例如，对 "2024/11/08" 查找 "/" 会得到 "2024" 的一个视图。
    /// </summary>
    /// <param name="source">源文本的只读跨度。</param>
    /// <param name="delimiter">要查找的分隔符。</param>
    /// <param name="comparisonType">指定搜索规则的枚举值。</param>
    /// <returns>
    /// 一个表示原始文本一部分的 <see cref="string"/>。<br/>
    /// 如果未找到分隔符，则返回代表整个原始文本的跨度。
    /// </returns>
    public static ReadOnlySpan<char> BeforeFirst(
        ReadOnlySpan<char> source,
        ReadOnlySpan<char> delimiter,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        var pos = source.IndexOf(delimiter, comparisonType);

        return pos >= 0 ? source[..pos] : source;
    }

    /// <summary>
    /// 获取在指定分隔符第一次出现之前的部分，返回一个零分配的视图。<br/>
    /// 例如，对 "2024/11/08" 查找 "/" 会得到 "2024" 的一个视图。
    /// </summary>
    /// <param name="source">源文本的只读跨度。</param>
    /// <param name="delimiter">要查找的分隔符。</param>
    /// <returns>
    /// 一个表示原始文本一部分的 <see cref="string"/>。<br/>
    /// 如果未找到分隔符，则返回代表整个原始文本的跨度。
    /// </returns>
    public static ReadOnlySpan<char> BeforeFirst(ReadOnlySpan<char> source, char delimiter)
    {
        var pos = source.IndexOf(delimiter);
        return pos >= 0 ? source[..pos] : source;
    }

    /// <summary>
    /// 获取在指定分隔符第一次出现之前的部分，返回一个零分配的视图。
    /// </summary>
    public static string BeforeFirst(
        this string source,
        string delimiter,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        return BeforeFirst(source.AsSpan(), delimiter.AsSpan(), comparisonType).ToString();
    }

    /// <summary>
    /// 获取在指定分隔符第一次出现之前的部分，返回一个零分配的视图。
    /// </summary>
    public static ReadOnlySpan<char> BeforeFirst(this string source, char delimiter)
    {
        return BeforeFirst(source.AsSpan(), delimiter);
    }

    /// <summary>
    /// 获取在指定分隔符最后一次出现之后的部分，返回一个零分配的视图。<br/>
    /// 例如，对 "a/b/c.txt" 查找 "/" 会得到 "c.txt" 的一个视图。
    /// </summary>
    /// <param name="source">源文本的只读跨度。</param>
    /// <param name="delimiter">要查找的分隔符。</param>
    /// <param name="comparisonType">指定搜索规则的枚举值。</param>
    /// <returns>
    /// 一个表示原始文本一部分的 <see cref="string"/>。<br/>
    /// 如果未找到分隔符，则返回代表整个原始文本的跨度。
    /// </returns>
    public static ReadOnlySpan<char> AfterLast(
        ReadOnlySpan<char> source,
        ReadOnlySpan<char> delimiter,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        var pos = source.LastIndexOf(delimiter, comparisonType);

        return pos >= 0 ? source[(pos + delimiter.Length)..] : source;
    }

    /// <summary>
    /// 获取在指定分隔符最后一次出现之后的部分，返回一个零分配的视图。<br/>
    /// 例如，对 "a/b/c.txt" 查找 "/" 会得到 "c.txt" 的一个视图。
    /// </summary>
    /// <param name="source">源文本的只读跨度。</param>
    /// <param name="delimiter">要查找的分隔符。</param>
    /// <returns>
    /// 一个表示原始文本一部分的 <see cref="string"/>。<br/>
    /// 如果未找到分隔符，则返回代表整个原始文本的跨度。
    /// </returns>
    public static ReadOnlySpan<char> AfterLast(ReadOnlySpan<char> source, char delimiter)
    {
        var pos = source.LastIndexOf(delimiter);
        return pos >= 0 ? source[(pos + 1)..] : source;
    }

    /// <summary>
    /// 获取在指定分隔符第一次出后之前的部分，返回一个零分配的视图。
    /// </summary>
    public static string AfterLast(
        this string source,
        string delimiter,
        StringComparison comparisonType = StringComparison.Ordinal)
    {
        return AfterLast(source.AsSpan(), delimiter.AsSpan(), comparisonType).ToString();
    }

    /// <summary>
    /// 获取在指定分隔符第一次出后之前的部分，返回一个零分配的视图。
    /// </summary>
    public static ReadOnlySpan<char> AfterLast(this string source, char delimiter)
    {
        return AfterLast(source.AsSpan(), delimiter);
    }
}