using System;

namespace PCL.Core.Model.ResourceProject.Modrinth;

[Serializable]
public record ModrinthModeratorMessage(
    string message,
    string? body);