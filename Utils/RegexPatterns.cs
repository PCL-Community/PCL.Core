﻿using System.Text.RegularExpressions;

namespace PCL.Core.Utils;

/// <summary>
/// 基于代码生成优化的正则表达式实例。
/// </summary>
public static partial class RegexPatterns
{
    /// <summary>
    /// 陶瓦联机 ID。
    /// </summary>
    public static readonly Regex TerracottaId = _TerracottaId();
    [GeneratedRegex("([0-9A-Z]{5}-){4}[0-9A-Z]{5}", RegexOptions.IgnoreCase)]
    private static partial Regex _TerracottaId();

    /// <summary>
    /// 换行符，包括 <c>\r\n</c> <c>\n</c> <c>\r</c> 三种。
    /// </summary>
    public static readonly Regex NewLine = _NewLine();
    [GeneratedRegex(@"\r\n|\n|\r", RegexOptions.Compiled)]
    private static partial Regex _NewLine();

    /// <summary>
    /// Semantic Versioning (SemVer) 规范的版本号，包含可选的 v 前缀。
    /// </summary>
    public static readonly Regex SemVer = _SemVer();
    private const string PatternSemVer =
        @"^v?(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)" +
        @"(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?" +
        @"(?:\+(?<build>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
    [GeneratedRegex(PatternSemVer, RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex _SemVer();

    /// <summary>
    /// 简单匹配 HTTP(S) URI，若需严格检查请使用 <see cref="FullHttpUri"/>。
    /// </summary>
    public static readonly Regex HttpUri = _HttpUri();
    private const string PatternHttpUri = @"^https?://(?:\[[^\]\s]+\]|[^/\s?#:]+)(?::\d{1,5})?(?:/[^\s?#]*)?(?:\?[^\s#]*)?(?:#\S*)?$";
    [GeneratedRegex(PatternHttpUri, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex _HttpUri();

    /// <summary>
    /// 包含完整规则的 HTTP(S) URI，含有 <c>scheme</c> <c>host</c> <c>ipv6</c>
    /// <c>port</c> <c>path</c> <c>query</c> <c>fragment</c> 分组。
    /// </summary>
    public static readonly Regex FullHttpUri = _FullHttpUri();
    private const string PatternFullHttpUri =
        @"^(?<scheme>https?)://(?<host>localhost|(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)(?:\.(?:25[0-5]|2[0-4]\d|1?\d?\d)){3})" +
        @"|\[(?<ipv6>[0-9A-Fa-f:.]+)\]|(?:(?:[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?\.)+(?:[A-Za-z]{2,63}|xn--[" +
        @"A-Za-z0-9\-]{2,59})))(?::(?<port>6553[0-5]|655[0-2]\d|65[0-4]\d{2}|6[0-4]\d{3}|[1-5]\d{4}|[1-9]\d{0,3}))?" +
        @"(?<path>/[^\s?#]*)?(?:\?(?<query>[^\s#]*))?(?:#(?<fragment>[^\s]*))?$";
    [GeneratedRegex(PatternFullHttpUri, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex _FullHttpUri();

    /// <summary>
    /// LastPending(_Xxx).log 路径。
    /// </summary>
    public static readonly Regex LastPendingLogPath = _LastPendingLogPath();
    [GeneratedRegex(@"\\LastPending[_]?[^\\]*\.log$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex _LastPendingLogPath();

    /// <summary>
    /// Mod loader 不兼容的错误提示。
    /// </summary>
    public static readonly Regex IncompatibleModLoaderErrorHint = _IncompatibleModLoaderErrorHint();
    [GeneratedRegex(@"(incompatible[\s\S]+'Fabric Loader' \(fabricloader\)|Mod ID: '(?:neo)?forge', Requested by '([^']+)')")]
    private static partial Regex _IncompatibleModLoaderErrorHint();

    /// <summary>
    /// Minecraft 颜色代码，为 Hex 颜色代码，格式为 <c>#RRGGBB</c>。
    /// </summary>
    public static readonly Regex HexColor = _HexColor();
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled)]
    private static partial Regex _HexColor();
    
    /// <summary>
    /// A compiled regular expression for matching Minecraft MOTD formatting codes.
    /// Matches legacy color/format codes (e.g., §a, §b, §k) and hexadecimal color codes (e.g., #FF0000).
    /// </summary>
    public static readonly Regex MotdCode = _MotdCode();
    [GeneratedRegex("(§[0-9a-fk-oAr]|#[0-9A-Fa-f]{6})", RegexOptions.Compiled)]
    private static partial Regex _MotdCode();
}
