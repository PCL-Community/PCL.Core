using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.Projects.Models;

public record ModrinthProjectDto
{
    // Modrinth API 可能返回 "id" 或 "project_id"
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("project_id")] public string? ProjectId { get; set; }

    public string EffectiveId => ProjectId ?? Id ?? throw new JsonException("Project ID is missing.");

    [JsonPropertyName("slug")] public required string Slug { get; set; }

    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("description")] public required string Description { get; set; }

    [JsonPropertyName("date_modified")] public DateTime DateModified { get; set; }

    [JsonPropertyName("downloads")] public int Downloads { get; set; }

    [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }

    [JsonPropertyName("project_type")] public required string ProjectType { get; set; }

    // API 可能返回 "versions" 或 "game_versions"
    [JsonPropertyName("versions")] public List<string>? Versions { get; set; }

    [JsonPropertyName("game_versions")] public List<string>? GameVersions { get; set; }

    public List<string> EffectiveGameVersions => GameVersions ?? Versions ?? [];

    [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = [];

    [JsonPropertyName("categories")] public List<string> Categories { get; set; } = [];
}