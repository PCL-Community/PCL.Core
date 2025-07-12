using System;

namespace PCL.Core.Model.Mod.Modrinth;

[Serializable]
public record ModrinthModeratorMessage(
    string message,
    string? body);