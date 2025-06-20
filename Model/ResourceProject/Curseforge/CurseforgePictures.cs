using System;

namespace PCL.Core.Model.ResourceProject.Curseforge;

[Serializable]
public record CurseforgePictures(
    int id,
    int modId,
    string title,
    string description,
    string thumbnailUrl,
    string url);