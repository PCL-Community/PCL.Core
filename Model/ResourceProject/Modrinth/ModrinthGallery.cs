using System;

namespace PCL.Core.Model.ResourceProject.Modrinth;

[Serializable]
public record ModrinthGallery(
    string url,
    bool featured,
    string? title,
    string? description,
    string created,
    int ordering);