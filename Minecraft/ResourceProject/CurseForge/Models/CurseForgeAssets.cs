using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeAsset
{
    [JsonPropertyName("id")] public int Id;
    [JsonPropertyName("modId")] public int ModId;
    [JsonPropertyName("title")] public required string Title;
    [JsonPropertyName("description")] public required string Description;
    [JsonPropertyName("thumbnailUrl")] public required string ThumbnailUrl;
    [JsonPropertyName("url")] public required string Url;
};