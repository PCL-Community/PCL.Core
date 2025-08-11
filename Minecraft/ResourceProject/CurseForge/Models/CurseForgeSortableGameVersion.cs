using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeSortableGameVersion
{
    [JsonPropertyName("gameVersionName")] public required string GameVersionName;
    [JsonPropertyName("gameVersionPadded")] public required string GameVersionPadded;
    [JsonPropertyName("gameVersion")] public required string GameVersion;
    [JsonPropertyName("gameVersionReleaseDate")] public required string GameVersionReleaseDate;
    [JsonPropertyName("gameVersionTypeId")] public int GameVersionTypeId;
};