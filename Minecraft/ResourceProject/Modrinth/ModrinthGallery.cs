using System;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

[Serializable]
public record ModrinthGallery(
    string Url,
    bool Featured,
    string? Title,
    string? Description,
    string Created,
    int Ordering);