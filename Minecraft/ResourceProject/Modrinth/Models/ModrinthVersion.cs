using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Model;

public record ModrinthVersion
{
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("version_number")] public required string VersionNumber;
    [JsonPropertyName("changelog")] public required string ChangeLog;
    [JsonPropertyName("Dependcies")] public required List<ModrinthDependcy?> Dependcies;
    [JsonPropertyName("game_versions")] public required List<string> GameVersions;
    [JsonPropertyName("version_type")] public required string VersionType;
    [JsonPropertyName("loaders")] public required List<string> Loaders;
    [JsonPropertyName("featured")] public required string Status;
    [JsonPropertyName("requested_status")] public string? RequestedStatus;
    [JsonPropertyName("id")] public required string Id;
    [JsonPropertyName("project_id")] public required string ProjectId;
    [JsonPropertyName("author_id")] public required string AuthorId;
    [JsonPropertyName("date_published")] public required string DatePublished;
    [JsonPropertyName("downloads")] public int DownloadCount;
    [JsonPropertyName("changelog_url")] public string? ChangeLogUrl;
    
};