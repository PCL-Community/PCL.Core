using System.Collections.Generic;

namespace PCL.Core.Utils.ModPlatform;

public class ModrinthFactsBuilder
{
    private List<string> _content = [];
    
    public ModrinthFactsBuilder EqualProjectType(ModProjectType projectType)
    {
        _content.Add($"[\"project_type:{ModrinthPlatformService.ProjectType2String(projectType)}\"]");
        return this;
    }

    public ModrinthFactsBuilder EqualGameVersion(string gameVersion)
    {
        _content.Add($"[\"versions:{gameVersion}\"]");
        return this;
    }

    public ModrinthFactsBuilder EqualGameVersion(IEnumerable<string> gameVersions)
    {
        foreach (var version in gameVersions)
            EqualGameVersion(version);
        return this;
    }

    public ModrinthFactsBuilder EqualCategories(string categories)
    {
        _content.Add($"[\"categories:{categories}\"]");
        return this;
    }

    public ModrinthFactsBuilder EqualCategories(IEnumerable<string> categories)
    {
        foreach (var category in categories)
            EqualCategories(category);
        return this;
    }

    public ModrinthFactsBuilder EqualLicense(string license)
    {
        _content.Add($"[\"license:{license}\"]");
        return this;
    }
    
    public override string ToString()
    {
        var ret = $"[{string.Join(",", _content)}]";
        return ret;
    }
}