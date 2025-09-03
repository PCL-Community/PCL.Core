using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PCL.Core.Minecraft.McInstance;

public interface IVersionComparer : IComparer<string>;

public class ReleaseVersionComparer : IVersionComparer
{
    public int Compare(string x, string y)
    {
        // 格式如 "1.20.1"，按数字分段比较
        var xParts = x.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        var yParts = y.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToArray();
        for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            if (xParts[i] != yParts[i])
                return xParts[i].CompareTo(yParts[i]);
        return xParts.Length.CompareTo(yParts.Length);
    }
}

public class SnapshotVersionComparer : IVersionComparer
{
    public int Compare(string x, string y)
    {
        // 格式如 "24w14a"，按年、周、子版本比较
        var regex = new Regex(@"(\d+)w(\d+)([a-z]?)");
        var xMatch = regex.Match(x);
        var yMatch = regex.Match(y);
        int xYear = xMatch.Success ? int.Parse(xMatch.Groups[1].Value) : 0;
        int yYear = yMatch.Success ? int.Parse(yMatch.Groups[1].Value) : 0;
        int xWeek = xMatch.Success ? int.Parse(xMatch.Groups[2].Value) : 0;
        int yWeek = yMatch.Success ? int.Parse(yMatch.Groups[2].Value) : 0;
        string xSub = xMatch.Success ? xMatch.Groups[3].Value : "";
        string ySub = yMatch.Success ? yMatch.Groups[3].Value : "";

        if (xYear != yYear) return xYear.CompareTo(yYear);
        if (xWeek != yWeek) return xWeek.CompareTo(yWeek);
        return xSub.CompareTo(ySub);
    }
}

public class OldVersionComparer : IVersionComparer
{
    public int Compare(string x, string y)
    {
        // 格式如 "alpha_1.2.3" 或 "beta_1.8"，按名称和数字比较
        var xParts = x.Split('_');
        var yParts = y.Split('_');
        var nameCompare = xParts[0].CompareTo(yParts[0]);
        if (nameCompare != 0) return nameCompare;
        return new ReleaseVersionComparer().Compare(xParts.Length > 1 ? xParts[1] : "", yParts.Length > 1 ? yParts[1] : "");
    }
}

public class PatcherVersionComparer : IVersionComparer
{
    public int Compare(string x, string y)
    {
        // 假设 NeoForge, Fabric 等版本格式类似 Release（如 "1.20.1" 或 "0.14.22"）
        return new ReleaseVersionComparer().Compare(x, y);
    }
}

public class ReleaseTimeComparer : IVersionComparer
{
    private readonly Func<McInstance, DateTime> _getReleaseTime;

    public ReleaseTimeComparer(Func<McInstance, DateTime> getReleaseTime)
    {
        _getReleaseTime = getReleaseTime;
    }

    public int Compare(string x, string y) => 0; // 占位符，实际比较在排序时使用 _getReleaseTime
}

public static class McVersionComparerFactory
{
    private static readonly Dictionary<McInstanceCardType, IVersionComparer> Comparers = new()
    {
        { McInstanceCardType.Release, new ReleaseVersionComparer() },
        { McInstanceCardType.Snapshot, new SnapshotVersionComparer() },
        { McInstanceCardType.Fool, new SnapshotVersionComparer() }, // 假设 Fool 格式类似 Snapshot
        { McInstanceCardType.Old, new OldVersionComparer() },
        { McInstanceCardType.Star, new ReleaseTimeComparer(instance => instance.GetVersionInfo()!.ReleaseTime) },
        { McInstanceCardType.Custom, new ReleaseTimeComparer(instance => instance.GetVersionInfo()!.ReleaseTime) },
        { McInstanceCardType.UnknownPatchers, new ReleaseTimeComparer(instance => instance.GetVersionInfo()!.ReleaseTime) },
        { McInstanceCardType.NeoForge, new PatcherVersionComparer() },
        { McInstanceCardType.Fabric, new PatcherVersionComparer() },
        { McInstanceCardType.Forge, new PatcherVersionComparer() },
        { McInstanceCardType.Quilt, new PatcherVersionComparer() },
        { McInstanceCardType.LegacyFabric, new PatcherVersionComparer() },
        { McInstanceCardType.Cleanroom, new PatcherVersionComparer() },
        { McInstanceCardType.LiteLoader, new PatcherVersionComparer() },
        { McInstanceCardType.OptiFine, new PatcherVersionComparer() },
        { McInstanceCardType.LabyMod, new PatcherVersionComparer() },
        { McInstanceCardType.Modded, new PatcherVersionComparer() },
        { McInstanceCardType.Client, new PatcherVersionComparer() }
    };

    public static IVersionComparer GetComparer(McInstanceCardType type) =>
        Comparers.TryGetValue(type, out var comparer) ? comparer : Comparers[McInstanceCardType.UnknownPatchers];
}