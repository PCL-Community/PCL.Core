using System;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeLinks(
    string WebsiteUrl,
    string WikiUrl,
    string IssuesUrl,
    string SourceUrl);