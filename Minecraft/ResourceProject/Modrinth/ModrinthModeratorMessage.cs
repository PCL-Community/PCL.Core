using System;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

[Serializable]
public record ModrinthModeratorMessage(
    string Message,
    string? Body);