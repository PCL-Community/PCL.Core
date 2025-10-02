using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Entities;

public record CacheProjectDto
{
    [JsonPropertyName("dataSource")] public required string DataSource { get; set; }
    [JsonPropertyName("type")] public required CompType Type { get; set; }
    [JsonPropertyName("slug")] public required string Slug { get; set; }
    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("curseFrogeFileIds")]
    public required List<int> CurseForgeFileIds { get; set; }

    [JsonPropertyName("rawName")] public required string RawName { get; set; }
    [JsonPropertyName("description")] public required string Description { get; set; }
    [JsonPropertyName("website")] public required string Website { get; set; }
    [JsonPropertyName("lastUpdate")] public required DateTime LastUpdate { get; set; }
    [JsonPropertyName("downloadCount")] public required int DownloadCount { get; set; }
    [JsonPropertyName("modLoaders")] public List<LoaderType>? ModLoaders { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
    [JsonPropertyName("gameVersions")] public List<int>? GameVersions { get; set; }
}