using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Model;

public record ModrinthFile
{
    [JsonPropertyName("hashes")] public required ModrinthHashes Hashes;
    [JsonPropertyName("url")] public required string Url;
    [JsonPropertyName("filename")] public required string FileName;
    [JsonPropertyName("primary")] public bool Primary;
    [JsonPropertyName("file_type")] public string? FileType;
}