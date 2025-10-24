using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record CurseForgeProjectFileDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("modId")] public required string ProjectId { get; init; }
    [JsonPropertyName("displayName")] public required string DisplayName { get; init; }
    [JsonPropertyName("fileDate")] public required DateTime ReleaseDate { get; init; }
    [JsonPropertyName("releaseType")] public required ProjectFileStatus ReleaseType { get; init; }
    [JsonPropertyName("downloadCount")] public required int DownloadCount { get; init; }
    [JsonPropertyName("fileName")] public required string FileName { get; init; }
    [JsonPropertyName("downloadUrl")] public string? DownloadUrl { get; init; }
    [JsonPropertyName("hashes")] public required List<CurseForgeHashDto> Hashes { get; init; }
    [JsonPropertyName("dependencies")] public List<CurseForgeFileDependencyDto>? Dependencies { get; init; }
    [JsonPropertyName("gameVersions")] public required List<string>? GameVersions { get; init; }
}

public record CurseForgeHashDto
{
    [JsonPropertyName("algo")] public required int Algo { get; init; }
    [JsonPropertyName("value")] public required string Value { get; init; }
}

public record CurseForgeFileDependencyDto
{
    [JsonPropertyName("relationtType")] public required int RelationType { get; init; }
    [JsonPropertyName("modId")] public required int ModId { get; init; }
}