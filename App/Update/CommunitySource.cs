using System.Collections.Generic;

namespace PCL.Core.App.Update;

public class CommunitySource(string name, string baseUrl) : IUpdateSource
{
    public string Name { get; } = name;
    public string BaseUrl { get; } = baseUrl;
    public HashSet<SourceAbility> Abilities { get => [SourceAbility.Update, SourceAbility.Announcement, SourceAbility.HotUpdate]; }
}