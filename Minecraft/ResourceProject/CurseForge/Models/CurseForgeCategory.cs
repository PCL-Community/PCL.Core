using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeCategory
{
    [JsonPropertyName("id")] public int Id;
    [JsonPropertyName("gameId")] public int GameId;
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("slug")] public required string Slug;
    [JsonPropertyName("url")] public required string Url;
    [JsonPropertyName("iconUrl")] public required string IconUrl;
    [JsonPropertyName("dateModified")] public required string DateModified;
    [JsonPropertyName("isClass")] public bool? IsClass;
    [JsonPropertyName("classId")] public int? ClassId;
    [JsonPropertyName("parentCategoryId")] public int? ParentCategoryId;
    [JsonPropertyName("displayIndex")] public int? DisplayIndex;
};