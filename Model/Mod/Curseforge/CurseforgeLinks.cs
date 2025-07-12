using System;

namespace PCL.Core.Model.Mod.Curseforge;

[Serializable]
public record CurseforgeLinks(
    string websiteUrl,
    string wikiUrl,
    string issuesUrl,
    string sourceUrl);