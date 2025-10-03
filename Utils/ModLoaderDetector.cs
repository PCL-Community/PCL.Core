using System.Collections.Generic;
using System.Linq;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Utils;

public static class ModLoaderDetector
{
    private static readonly HashSet<string> _PluginKeywords = ["bukket", "folia", "paper", "purpur", "spigot"];

    private static readonly Dictionary<string, LoaderType> _LoaderMap = new()
    {
        { "forge", LoaderType.Forge },
        { "neoforge", LoaderType.NeoForge },
        { "fabric", LoaderType.Fabric },
        { "quilt", LoaderType.Quilt }
    };

    public static (CompType type, List<LoaderType> loaders)
        DetectModrinthType(CompType defaultType, IReadOnlyList<string> rawLoaders)
    {
        var loaderList = rawLoaders?.ToHashSet() ?? [];

        CompType finalType;
        if (loaderList.Any(loader => _PluginKeywords.Contains(loader)))
        {
            finalType = CompType.Plugin;
        }
        else if (loaderList.Any(loader => _LoaderMap.ContainsKey(loader)))
        {
            finalType = CompType.Mod;
        }
        else if (loaderList.Contains("datapack"))
        {
            finalType = CompType.DataPack;
        }
        else
        {
            finalType = defaultType;
        }

        var modLoaders = loaderList.Where(loader => _LoaderMap.ContainsKey(loader))
            .Select(loader => _LoaderMap[loader])
            .Distinct()
            .ToList();

        return (finalType, modLoaders);
    }

    public static List<LoaderType> DetechCurseForgeType(HashSet<string> rawGameVers)
    {
        List<LoaderType> types = [];

        if (rawGameVers.Count == 0)
        {
            return types;
        }

        if (rawGameVers.Contains("forge"))
        {
            types.Add(LoaderType.Forge);
        }

        if (rawGameVers.Contains("fabric"))
        {
            types.Add(LoaderType.Fabric);
        }

        if (rawGameVers.Contains("quilt"))
        {
            types.Add(LoaderType.Quilt);
        }

        if (rawGameVers.Contains("neoforge"))
        {
            types.Add(LoaderType.NeoForge);
        }

        return types;
    }
}