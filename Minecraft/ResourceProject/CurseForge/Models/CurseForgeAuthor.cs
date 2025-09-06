using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeAuthor
{
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("id")] public int Id;
    [JsonPropertyName("url")] public required string Url;
}