using System;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeAuthors(
    int Id,
    string Name,
    string Url);