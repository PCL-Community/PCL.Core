using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace PCL.Core.Minecraft.McInstance;

public class ReleaseVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y) {
        if (x == null || y == null) return 0;
        return Version.Parse(x).CompareTo(Version.Parse(y));
    }
}

public class SnapshotVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y) {
        if (x == null || y == null) return 0;

        if (!x.Contains('w') || !y.Contains('w')) {
            return 0;
        }
        // 格式如 "24w14a"，按年、周、子版本比较
        var regex = new Regex(@"(\d+)w(\d+)([a-z]?)");
        var xMatch = regex.Match(x);
        var yMatch = regex.Match(y);
        var xYear = xMatch.Success ? int.Parse(xMatch.Groups[1].Value) : 0;
        var yYear = yMatch.Success ? int.Parse(yMatch.Groups[1].Value) : 0;
        var xWeek = xMatch.Success ? int.Parse(xMatch.Groups[2].Value) : 0;
        var yWeek = yMatch.Success ? int.Parse(yMatch.Groups[2].Value) : 0;
        var xSub = xMatch.Success ? xMatch.Groups[3].Value : "";
        var ySub = yMatch.Success ? yMatch.Groups[3].Value : "";

        if (xYear != yYear) return xYear.CompareTo(yYear);
        if (xWeek != yWeek) return xWeek.CompareTo(yWeek);
        return StringComparer.Ordinal.Compare(xSub, ySub);
    }
}

public class OldVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y) {
        if (x == null || y == null) return 0;

        // 获取阶段顺序和可比较键
        var (xOrder, xKey, xRaw) = GetSortKey(x);
        var (yOrder, yKey, yRaw) = GetSortKey(y);

        // 先比较阶段顺序
        if (xOrder != yOrder) return xOrder.CompareTo(yOrder);

        // 阶段内比较键
        int keyCompare = CompareKeys(xKey, yKey);
        if (keyCompare != 0) return keyCompare;

        // 如果键比较相等，回退到字符串序比较
        return StringComparer.Ordinal.Compare(xRaw, yRaw);
    }

    private (int order, object key, string raw) GetSortKey(string version) {
        string raw = version; // 保存原始字符串
        version = version.Trim().ToLowerInvariant(); // 规范化

        // pre-Classic: rd-131655, 解析数字
        if (version.StartsWith("rd-")) {
            var numStr = version.Substring(3);
            if (int.TryParse(numStr, out var num))
                return (0, num, raw);
            return (0, 0, raw);
        }

        // Classic: c0.0.11a, 按 a/st/其他优先级 + 版本号
        if (version.StartsWith("c")) {
            var priority = 2;
            if (version.Contains("a"))
                priority = 0;
            else if (version.Contains("st"))
                priority = 1;
            return (1, priority, raw);
        }

        // Indev: in-20100218 或 in-20100214-2, 解析日期 + 后缀
        var indevRegex = new Regex(@"^in-(\d{8})(-(\d+))?$");
        var indevMatch = indevRegex.Match(version);
        if (indevMatch.Success) {
            if (DateTime.TryParseExact(indevMatch.Groups[1].Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date)) {
                var suffixNum = indevMatch.Groups[3].Success ? int.Parse(indevMatch.Groups[3].Value) : 0;
                return (2, (date, suffixNum), raw);
            }
            return (2, (DateTime.MinValue, 0), raw);
        }

        // Infdev: inf-20100630 或 inf-20100630-1, 解析日期 + 后缀
        var infdevRegex = new Regex(@"^inf-(\d{8})(-(\d+))?$");
        var infdevMatch = infdevRegex.Match(version);
        if (infdevMatch.Success) {
            if (DateTime.TryParseExact(infdevMatch.Groups[1].Value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date)) {
                var suffixNum = infdevMatch.Groups[3].Success ? int.Parse(infdevMatch.Groups[3].Value) : 0;
                return (3, (date, suffixNum), raw);
            }
            return (3, (DateTime.MinValue, 0), raw);
        }

        // Alpha: a1.0.1 或 a1.0.1_01, 解析版本号
        if (version.StartsWith("a")) {
            var alphaParts = version.Split('-')[0].Split('_')[0].Substring(1); // 去掉 'a'
            if (Version.TryParse(alphaParts, out var ver))
                return (4, ver, raw);
            return (4, alphaParts, raw);
        }

        // Beta: b1.0 或 b1.0_01, 解析版本号
        if (version.StartsWith("b")) {
            var betaParts = version.Split('-')[0].Split('_')[0].Substring(1); // 去掉 'b'
            if (Version.TryParse(betaParts, out var ver))
                return (5, ver, raw);
            return (5, betaParts, raw);
        }

        // 不匹配默认按字符串序
        return (99, version, raw);
    }

    private int CompareKeys(object xKey, object yKey) {
        // pre-Classic 数字比较
        if (xKey is int xInt && yKey is int yInt)
            return xInt.CompareTo(yInt);

        // Indev/Infdev (DateTime, suffixNum)
        if (xKey is ValueTuple<DateTime, int> xIndev && yKey is ValueTuple<DateTime, int> yIndev) {
            var dateCompare = xIndev.Item1.CompareTo(yIndev.Item1);
            if (dateCompare != 0) return dateCompare;
            return xIndev.Item2.CompareTo(yIndev.Item2);
        }

        // Alpha/Beta Version 比较
        if (xKey is Version xVer && yKey is Version yVer)
            return xVer.CompareTo(yVer);

        // 字符串比较
        return StringComparer.Ordinal.Compare(xKey.ToString(), yKey.ToString());
    }
}

public class FoolVersionComparer : IComparer<string> {
    private static readonly ImmutableList<string> FoolVersions = ImmutableList.Create<string>(
        "2point0_red",
        "2point0_blue",
        "2point0_purple",
        "2.0_red",
        "2.0_blue",
        "2.0_purple",
        "15w14a",
        "1.rv-pre1",
        "3d shareware v1.34",
        "20w14∞",
        "22w13oneblockatatime",
        "23w13a_or_b",
        "24w14potato",
        "25w14craftmine"
        );

    public int Compare(string? x, string? y) {
        if (x == null || y == null) return 0;
        // 直接按字符串序比较
        return FoolVersions.IndexOf(x).CompareTo(FoolVersions.IndexOf(y));
    }
}

public class NeoForgeVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null) return 0;
        
        if (string.IsNullOrEmpty(x) && string.IsNullOrEmpty(y)) return 0;
        if (string.IsNullOrEmpty(x)) return 1;
        if (string.IsNullOrEmpty(y)) return -1;

        // 分割版本号和后缀
        var xVersionNum = x;
        var yVersionNum = y;
        var xSuffix = "";
        var ySuffix = "";
        var xSuffixIndex = x.IndexOf('-');
        if (xSuffixIndex >= 0)
        {
            xVersionNum = x.Substring(0, xSuffixIndex);
            xSuffix = x.Substring(xSuffixIndex + 1);
        }
        var ySuffixIndex = y.IndexOf('-');
        if (ySuffixIndex >= 0)
        {
            yVersionNum = y.Substring(0, ySuffixIndex);
            ySuffix = y.Substring(ySuffixIndex + 1);
        }

        // 比较数字部分
        var xParts = xVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = yVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        for (var i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
                return xParts[i].CompareTo(yParts[i]);
        }
        if (xParts.Length != yParts.Length)
            return xParts.Length.CompareTo(yParts.Length);

        // 无后缀优于有后缀
        if (string.IsNullOrEmpty(xSuffix) && !string.IsNullOrEmpty(ySuffix)) return -1;
        if (!string.IsNullOrEmpty(xSuffix) && string.IsNullOrEmpty(ySuffix)) return 1;

        // 后缀比较
        if (!string.IsNullOrEmpty(xSuffix) && !string.IsNullOrEmpty(ySuffix))
            return StringComparer.Ordinal.Compare(xSuffix, ySuffix);

        // 数字和后缀相等，回退到原始字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class FabricVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 分割版本号和后缀
        string xVersionNum = x;
        string yVersionNum = y;
        string xBuild = "";
        string yBuild = "";
        int xBuildIndex = x.IndexOf('+');
        if (xBuildIndex >= 0)
        {
            xVersionNum = x.Substring(0, xBuildIndex);
            xBuild = x.Substring(xBuildIndex + 1);
        }
        int yBuildIndex = y.IndexOf('+');
        if (yBuildIndex >= 0)
        {
            yVersionNum = y.Substring(0, yBuildIndex);
            yBuild = y.Substring(yBuildIndex + 1);
        }

        // 解析数字部分
        var xParts = xVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = yVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();

        // 逐段比较数字
        for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
                return xParts[i].CompareTo(yParts[i]);
        }
        if (xParts.Length != yParts.Length)
            return xParts.Length.CompareTo(yParts.Length);

        // 无 build 优于有 build
        if (string.IsNullOrEmpty(xBuild) && !string.IsNullOrEmpty(yBuild)) return -1;
        if (!string.IsNullOrEmpty(xBuild) && string.IsNullOrEmpty(yBuild)) return 1;

        // 比较 build 部分（仅中间格式，如 "build.214"）
        if (!string.IsNullOrEmpty(xBuild) && !string.IsNullOrEmpty(yBuild))
        {
            // 提取 build 号（如 "build.214" -> 214）
            int xBuildNum = int.TryParse(xBuild.Replace("build.", ""), out int n) ? n : 0;
            int yBuildNum = int.TryParse(yBuild.Replace("build.", ""), out int m) ? m : 0;
            if (xBuildNum != yBuildNum)
                return xBuildNum.CompareTo(yBuildNum);
        }

        // 数字和 build 相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class ForgeVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 分割版本号
        var xParts = x.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = y.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();

        // 逐段比较数字，最多比较4段（major.minor.patch.revision）
        for (int i = 0; i < Math.Min(Math.Max(xParts.Length, yParts.Length), 4); i++)
        {
            int xValue = i < xParts.Length ? xParts[i] : 0; // 三位版本补0
            int yValue = i < yParts.Length ? yParts[i] : 0; // 三位版本补0
            if (xValue != yValue)
                return xValue.CompareTo(yValue);
        }

        // 数字部分相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class QuiltVersionComparer : IComparer<string>{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 去除末尾斜杠
        string xStr = x.EndsWith("/") ? x.Substring(0, x.Length - 1) : x;
        string yStr = y.EndsWith("/") ? y.Substring(0, y.Length - 1) : y;

        // 分割版本号和 beta 后缀
        string xVersionNum = xStr;
        string yVersionNum = yStr;
        string xBeta = "";
        string yBeta = "";
        int xBetaIndex = xStr.IndexOf("-beta.");
        if (xBetaIndex >= 0)
        {
            xVersionNum = xStr.Substring(0, xBetaIndex);
            xBeta = xStr.Substring(xBetaIndex + 6); // 跳过 "-beta."
        }
        int yBetaIndex = yStr.IndexOf("-beta.");
        if (yBetaIndex >= 0)
        {
            yVersionNum = yStr.Substring(0, yBetaIndex);
            yBeta = yStr.Substring(yBetaIndex + 6); // 跳过 "-beta."
        }

        // 解析数字部分
        var xParts = xVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = yVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();

        // 逐段比较数字
        for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
                return xParts[i].CompareTo(yParts[i]);
        }
        if (xParts.Length != yParts.Length)
            return xParts.Length.CompareTo(yParts.Length);

        // 稳定版优于 beta 版
        if (string.IsNullOrEmpty(xBeta) && !string.IsNullOrEmpty(yBeta)) return -1;
        if (!string.IsNullOrEmpty(xBeta) && string.IsNullOrEmpty(yBeta)) return 1;

        // 比较 beta revision
        if (!string.IsNullOrEmpty(xBeta) && !string.IsNullOrEmpty(yBeta))
        {
            int xBetaNum = int.TryParse(xBeta, out int n) ? n : 0;
            int yBetaNum = int.TryParse(yBeta, out int m) ? m : 0;
            if (xBetaNum != yBetaNum)
                return xBetaNum.CompareTo(yBetaNum);
        }

        // 所有部分相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(xStr, yStr);
    }
}

public class CleanroomVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 分割版本号和 alpha 后缀
        string xVersionNum = x;
        string yVersionNum = y;
        string xAlpha = "";
        string yAlpha = "";
        int xAlphaIndex = x.IndexOf("-alpha");
        if (xAlphaIndex >= 0)
        {
            xVersionNum = x.Substring(0, xAlphaIndex);
            xAlpha = x.Substring(xAlphaIndex + 6); // 跳过 "-alpha"
        }
        int yAlphaIndex = y.IndexOf("-alpha");
        if (yAlphaIndex >= 0)
        {
            yVersionNum = y.Substring(0, yAlphaIndex);
            yAlpha = y.Substring(yAlphaIndex + 6); // 跳过 "-alpha"
        }

        // 解析数字部分
        var xParts = xVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = yVersionNum.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();

        // 逐段比较数字
        for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            if (xParts[i] != yParts[i])
                return xParts[i].CompareTo(yParts[i]);
        }
        if (xParts.Length != yParts.Length)
            return xParts.Length.CompareTo(yParts.Length);

        // 稳定版优于 alpha 版
        if (string.IsNullOrEmpty(xAlpha) && !string.IsNullOrEmpty(yAlpha)) return -1;
        if (!string.IsNullOrEmpty(xAlpha) && string.IsNullOrEmpty(yAlpha)) return 1;

        // alpha 后缀比较（通常为空）
        if (!string.IsNullOrEmpty(xAlpha) && !string.IsNullOrEmpty(yAlpha))
            return StringComparer.Ordinal.Compare(xAlpha, yAlpha);

        // 所有部分相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class LiteLoaderVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 分割时间戳和构建编号
        string xTimestamp = x;
        string yTimestamp = y;
        string xBuildNum = "";
        string yBuildNum = "";
        int xDashIndex = x.IndexOf('-');
        if (xDashIndex >= 0)
        {
            xTimestamp = x.Substring(0, xDashIndex);
            xBuildNum = x.Substring(xDashIndex + 1);
        }
        int yDashIndex = y.IndexOf('-');
        if (yDashIndex >= 0)
        {
            yTimestamp = y.Substring(0, yDashIndex);
            yBuildNum = y.Substring(yDashIndex + 1);
        }

        // 解析时间戳（YYYYMMDD.HHMMSS）
        var xTimeParts = xTimestamp.Split('.');
        var yTimeParts = yTimestamp.Split('.');
        if (xTimeParts.Length != 2 || yTimeParts.Length != 2)
            return StringComparer.Ordinal.Compare(x, y);

        // 解析 YYYYMMDD 和 HHMMSS
        var xDateParts = xTimeParts[0].Length == 8 ? new[]
        {
            int.TryParse(xTimeParts[0].Substring(0, 4), out var n1) ? n1 : 0, // YYYY
            int.TryParse(xTimeParts[0].Substring(4, 2), out n1) ? n1 : 0,     // MM
            int.TryParse(xTimeParts[0].Substring(6, 2), out n1) ? n1 : 0      // DD
        } : new int[3];
        var yDateParts = yTimeParts[0].Length == 8 ? new[]
        {
            int.TryParse(yTimeParts[0].Substring(0, 4), out var n2) ? n2 : 0, // YYYY
            int.TryParse(yTimeParts[0].Substring(4, 2), out n2) ? n2 : 0,     // MM
            int.TryParse(yTimeParts[0].Substring(6, 2), out n2) ? n2 : 0      // DD
        } : new int[3];
        var xTimeSubParts = xTimeParts[1].Length == 6 ? new[]
        {
            int.TryParse(xTimeParts[1].Substring(0, 2), out var n3) ? n3 : 0, // HH
            int.TryParse(xTimeParts[1].Substring(2, 2), out n3) ? n3 : 0,     // MM
            int.TryParse(xTimeParts[1].Substring(4, 2), out n3) ? n3 : 0      // SS
        } : new int[3];
        var yTimeSubParts = yTimeParts[1].Length == 6 ? new[]
        {
            int.TryParse(yTimeParts[1].Substring(0, 2), out var n4) ? n4 : 0, // HH
            int.TryParse(yTimeParts[1].Substring(2, 2), out n4) ? n4 : 0,     // MM
            int.TryParse(yTimeParts[1].Substring(4, 2), out n4) ? n4 : 0      // SS
        } : new int[3];

        // 比较时间戳（YYYY, MM, DD, HH, MM, SS）
        for (int i = 0; i < 3; i++)
        {
            if (xDateParts[i] != yDateParts[i])
                return xDateParts[i].CompareTo(yDateParts[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            if (xTimeSubParts[i] != yTimeSubParts[i])
                return xTimeSubParts[i].CompareTo(yTimeSubParts[i]);
        }

        // 比较构建编号
        int xBuild = int.TryParse(xBuildNum, out int n) ? n : 0;
        int yBuild = int.TryParse(yBuildNum, out int m) ? m : 0;
        if (xBuild != yBuild)
            return xBuild.CompareTo(yBuild);

        // 所有部分相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class OptiFineVersionComparer : IComparer<string> {
    public int Compare(string? x, string? y) {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        // 分割版本号
        string xVersion = x;
        string yVersion = y;
        string xPre = "";
        string yPre = "";
        int xPreIndex = x.IndexOf("_pre");
        if (xPreIndex >= 0)
        {
            xVersion = x.Substring(0, xPreIndex);
            xPre = x.Substring(xPreIndex + 4); // 跳过 "_pre"
        }
        int yPreIndex = y.IndexOf("_pre");
        if (yPreIndex >= 0)
        {
            yVersion = y.Substring(0, yPreIndex);
            yPre = y.Substring(yPreIndex + 4); // 跳过 "_pre"
        }

        // 去除固定前缀 HD_U_
        const string prefix = "HD_U_";
        if (!xVersion.StartsWith(prefix) || !yVersion.StartsWith(prefix))
            return StringComparer.Ordinal.Compare(x, y);
        xVersion = xVersion.Substring(prefix.Length);
        yVersion = yVersion.Substring(prefix.Length);

        // 解析主版本字母和次版本数字
        char xMain = xVersion.Length > 0 ? xVersion[0] : '\0';
        char yMain = yVersion.Length > 0 ? yVersion[0] : '\0';
        int xSub = xVersion.Length > 1 && int.TryParse(xVersion.Substring(1), out int n) ? n : 0;
        int ySub = yVersion.Length > 1 && int.TryParse(yVersion.Substring(1), out int m) ? m : 0;

        // 比较主版本字母（字母序小的靠前）
        if (xMain != yMain)
            return StringComparer.Ordinal.Compare(xMain.ToString(), yMain.ToString());

        // 比较次版本数字
        if (xSub != ySub)
            return xSub.CompareTo(ySub);

        // 稳定版优于预发布版
        if (string.IsNullOrEmpty(xPre) && !string.IsNullOrEmpty(yPre)) return -1;
        if (!string.IsNullOrEmpty(xPre) && string.IsNullOrEmpty(yPre)) return 1;

        // 比较预发布编号
        if (!string.IsNullOrEmpty(xPre) && !string.IsNullOrEmpty(yPre))
        {
            var xPreNum = int.TryParse(xPre, out var n1) ? n1 : 0;
            var yPreNum = int.TryParse(yPre, out var m1) ? m1 : 0;
            if (xPreNum != yPreNum)
                return xPreNum.CompareTo(yPreNum);
        }

        // 所有部分相等，回退到字符串比较
        return StringComparer.Ordinal.Compare(x, y);
    }
}

public class PatcherVersionComparer : IComparer<ValueTuple<McInstanceCardType, PatcherInfo>> {
    private static readonly Dictionary<McInstanceCardType, IComparer<string>> Comparers = new() {
        // 正常版本
        { McInstanceCardType.Release, McVersionComparerFactory.ReleaseVersionComparer },
        { McInstanceCardType.Snapshot, McVersionComparerFactory.SnapshotVersionComparer },
        { McInstanceCardType.Fool, McVersionComparerFactory.FoolVersionComparer },
        { McInstanceCardType.Old, McVersionComparerFactory.OldVersionComparer },
        // 模组加载器
        { McInstanceCardType.NeoForge, McVersionComparerFactory.NeoForgeVersionComparer },
        { McInstanceCardType.Fabric, McVersionComparerFactory.FabricVersionComparer },
        { McInstanceCardType.Forge, McVersionComparerFactory.ForgeVersionComparer },
        { McInstanceCardType.Quilt, McVersionComparerFactory.QuiltVersionComparer },
        { McInstanceCardType.LegacyFabric, McVersionComparerFactory.FabricVersionComparer },
        { McInstanceCardType.Cleanroom, McVersionComparerFactory.CleanroomVersionComparer },
        { McInstanceCardType.LiteLoader, McVersionComparerFactory.LiteLoaderVersionComparer },
        // 客户端
        { McInstanceCardType.OptiFine, McVersionComparerFactory.OptiFineVersionComparer },
        { McInstanceCardType.LabyMod, McVersionComparerFactory.ReleaseVersionComparer },
    };

    public int Compare(ValueTuple<McInstanceCardType, PatcherInfo> x, ValueTuple<McInstanceCardType, PatcherInfo> y) {
        var (xType, xInfo) = x;
        var (yType, yInfo) = y;

        if (xType is McInstanceCardType.Star or McInstanceCardType.Custom or McInstanceCardType.UnknownPatchers) {
            if (xInfo.ReleaseTime != null && yInfo.ReleaseTime != null) {
                return xInfo.ReleaseTime.Value.CompareTo(yInfo.ReleaseTime.Value);
            }
            return StringComparer.Ordinal.Compare(xInfo.Version, yInfo.Version);
        }

        if (xInfo.Version != null && yInfo.Version != null) {
            return Comparers[xType].Compare(xInfo.Version, yInfo.Version);
        }

        return 0;
    }
}

public static class McVersionComparerFactory {
    // 定义可复用的 PatcherVersionComparer
    public static IComparer<ValueTuple<McInstanceCardType, PatcherInfo>> PatcherVersionComparer { get; } = new PatcherVersionComparer();

    public static IComparer<string> ReleaseVersionComparer { get; } = new ReleaseVersionComparer();
    public static IComparer<string> SnapshotVersionComparer { get; } = new SnapshotVersionComparer();
    public static IComparer<string> OldVersionComparer { get; } = new OldVersionComparer();
    public static IComparer<string> FoolVersionComparer { get; } = new FoolVersionComparer();
    
    public static IComparer<string> NeoForgeVersionComparer { get; } = new NeoForgeVersionComparer();
    public static IComparer<string> FabricVersionComparer { get; } = new FabricVersionComparer();
    public static IComparer<string> ForgeVersionComparer { get; } = new ForgeVersionComparer();
    public static IComparer<string> QuiltVersionComparer { get; } = new QuiltVersionComparer();
    public static IComparer<string> CleanroomVersionComparer { get; } = new CleanroomVersionComparer();
    public static IComparer<string> LiteLoaderVersionComparer { get; } = new LiteLoaderVersionComparer();
    
    public static IComparer<string> OptiFineVersionComparer { get; } = new OptiFineVersionComparer();
}
