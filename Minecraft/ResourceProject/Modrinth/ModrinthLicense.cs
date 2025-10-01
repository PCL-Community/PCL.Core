using System;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

[Serializable]
public record ModrinthLicense(
    string Id,
    string Name,
    string? Url);