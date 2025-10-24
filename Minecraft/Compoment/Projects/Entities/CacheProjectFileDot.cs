using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record CacheProjectFileDot
{
    [JsonPropertyName("fromCurseForge")] public required bool FromCurseForge { get; init; }
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("displayName")] public required string DisplayName { get; init; }
    [JsonPropertyName("releaseDate")] public required DateTime ReleaseDate { get; init; }
    [JsonPropertyName("downloadCount")] public required int DownloadCount { get; init; }
    [JsonPropertyName("status")] public required ProjectFileStatus Status { get; init; }

    [JsonPropertyName("fileName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FileName { get; init; }

    [JsonPropertyName("downloadUrls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? DownloadUrls { get; init; }

    [JsonPropertyName("modLoaders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LoaderType>? ModLoaders { get; init; }

    [JsonPropertyName("hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hash { get; init; }

    [JsonPropertyName("gameVersions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? GameVersions { get; init; }

    [JsonPropertyName("rawDependencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RawDependencies { get; init; }

    [JsonPropertyName("dependencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Dependencies { get; init; }

    [JsonPropertyName("rawOptionalDependencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? RawOptionalDependencies { get; init; }

    [JsonPropertyName("optionalDependencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? OptionalDependencies { get; init; }
}