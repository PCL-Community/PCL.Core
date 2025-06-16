using System;

namespace PCL.Core.Model.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeLinks(
    string websiteUrl,
    string wikiUrl,
    string issuesUrl,
    string sourceUrl);