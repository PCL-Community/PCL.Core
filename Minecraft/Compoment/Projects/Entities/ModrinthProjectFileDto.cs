using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record ModrinthProjectFileDto
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("project_id")] public required string ProjectId { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("date_published")] public required DateTime ReleaseDate { get; init; }
    [JsonPropertyName("version_type")] public required string VersionType { get; init; }
    [JsonPropertyName("downloads")] public required int DownloadCount { get; init; }
    [JsonPropertyName("loaders")] public required List<string> Loaders { get; init; }
    [JsonPropertyName("files")] public List<ModrinthFileInfoDto>? Files { get; init; }
    [JsonPropertyName("dependencies")] public List<ModrinthFileDependencyDto>? Dependencies { get; init; }
    [JsonPropertyName("game_versions")] public required List<string> GameVersions { get; init; }
}

public record ModrinthFileInfoDto
{
    [JsonPropertyName("filename")] public required string FileName { get; init; }
    [JsonPropertyName("url")] public required string Url { get; init; }
    [JsonPropertyName("hashes")] public required Dictionary<string, string> Hashes { get; init; }
}

public record ModrinthFileDependencyDto
{
    [JsonPropertyName("dependency_type")] public required string Type { get; init; }
    [JsonPropertyName("project_id")] public required string ProjectId { get; init; }
}