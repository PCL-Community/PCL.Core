using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

public record ModrinthDependcy
{
    [JsonPropertyName("version_id")] public required string VersionId;
    [JsonPropertyName("project_id")] public required string ProjectId;
    [JsonPropertyName("file_name")] public required string FileName;
    [JsonPropertyName("dependency_type")] public required string DependencyType;
};