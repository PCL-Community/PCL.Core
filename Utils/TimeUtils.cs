namespace PCL.Core.Utils;

using System;

/// <summary>
/// 提供与时间相关的实用方法。
/// </summary>
public static class TimeUtils {
    /// <summary>
    /// 获取格式类似于“11:08:52.037”的当前时间字符串。
    /// </summary>
    /// <returns>格式化后的时间字符串。</returns>
    public static string GetTimeNow() {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }

    /// <summary>
    /// 获取系统运行时间（毫秒），保证为正长整型，且大于 1。
    /// </summary>
    /// <remarks>
    /// 此方法基于 Environment.TickCount，但在 .NET 框架中，我们有更可靠的替代方案。
    /// </remarks>
    /// <returns>系统运行毫秒数。</returns>
    public static long GetTimeTick() {
        // 原始代码处理了 Environment.TickCount 的符号溢出问题（在 24.8 天后），
        // 但在现代 .NET 中，我们有更可靠、更精确的 Stopwatch 类。
        // 为了保持原函数意图，这里直接返回 Environment.TickCount。
        // 注意：Environment.TickCount 在64位系统中可能为负，不推荐在重要场景使用。
        return Environment.TickCount64;
    }

    /// <summary>
    /// 将时间间隔转换为易于阅读的形式。
    /// </summary>
    /// <param name="span">要转换的时间间隔。</param>
    /// <param name="isShortForm">如果为 true，则使用简短格式。</param>
    /// <returns>格式化后的时间间隔字符串。</returns>
    public static string GetTimeSpanString(TimeSpan span, bool isShortForm) {
        var isPast = span.TotalMilliseconds < 0;
        var endFix = isPast ? "前" : "后";
        if (isPast) {
            span = span.Negate();
        }

        var totalMonths = Math.Floor(span.Days / 30.0);
        string result;

        if (isShortForm) {
            if (totalMonths >= 12) {
                result = $"{Math.Floor(totalMonths / 12)} 年";
            } else if (totalMonths >= 2) {
                result = $"{totalMonths} 个月";
            } else if (span.TotalDays >= 2) {
                result = $"{span.Days} 天";
            } else if (span.TotalHours >= 1) {
                result = $"{span.Hours} 小时";
            } else if (span.TotalMinutes >= 1) {
                result = $"{span.Minutes} 分钟";
            } else if (span.TotalSeconds >= 1) {
                result = $"{span.Seconds} 秒";
            } else {
                result = "1 秒";
            }
        } else // Long form
        {
            if (totalMonths >= 61) {
                result = $"{Math.Floor(totalMonths / 12)} 年";
            } else if (totalMonths >= 12) {
                var years = Math.Floor(totalMonths / 12);
                var months = totalMonths % 12;
                result = $"{years} 年{(months > 0 ? $" {months} 个月" : "")}";
            } else if (totalMonths >= 4) {
                result = $"{totalMonths} 个月";
            } else if (totalMonths >= 1) {
                var days = span.Days % 30;
                result = $"{totalMonths} 月{(days > 0 ? $" {days} 天" : "")}";
            } else if (span.TotalDays >= 4) {
                result = $"{span.Days} 天";
            } else if (span.TotalDays >= 1) {
                result = $"{span.Days} 天{(span.Hours > 0 ? $" {span.Hours} 小时" : "")}";
            } else if (span.TotalHours >= 10) {
                result = $"{span.Hours} 小时";
            } else if (span.TotalHours >= 1) {
                result = $"{span.Hours} 小时{(span.Minutes > 0 ? $" {span.Minutes} 分钟" : "")}";
            } else if (span.TotalMinutes >= 10) {
                result = $"{span.Minutes} 分钟";
            } else if (span.TotalMinutes >= 1) {
                result = $"{span.Minutes} 分{(span.Seconds > 0 ? $" {span.Seconds} 秒" : "")}";
            } else if (span.TotalSeconds >= 1) {
                result = $"{span.Seconds} 秒";
            } else {
                result = "1 秒";
            }
        }

        return result + endFix;
    }

    /// <summary>
    /// 获取十进制 Unix 时间戳（秒）。
    /// </summary>
    /// <returns>当前时间的 Unix 时间戳。</returns>
    public static long GetUnixTimestamp() {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
