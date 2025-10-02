using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PCL.Core.Minecraft.Compoment.Entities;

namespace PCL.Core.Utils;

internal static partial class GameVersionSorterUtil
{
    public static List<string> SortGameVersions(List<string> versions)
    {
        var sortedVers = versions
            .Select(ver => MinecraftVersion.TryParse(ver, out var version) ? version : null)
            .Where(ver => ver is not null)
            .OrderByDescending(ver => ver)
            .ToList();

        if (sortedVers.Count != 0)
        {
            var finalVers = sortedVers.Select(ver => ver!.ToString()).ToList();

            return finalVers;
        }

        var snapshotRegex = SnapshotRegex();
        var snapshotVers = versions.Where(ver => snapshotRegex.IsMatch(ver)).ToList();

        if (snapshotVers.Count != 0)
        {
            return snapshotVers;
        }

        return ["未知版本"];
    }

    [GeneratedRegex(@"[0-9]{2}w[0-9]{2}[a-z]{1}")]
    private static partial Regex SnapshotRegex();
}