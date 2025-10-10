using System;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgePictures(
    int Id,
    int ModId,
    string Title,
    string Description,
    string ThumbnailUrl,
    string Url);