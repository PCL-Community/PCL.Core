using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFileIndex
{
    [JsonPropertyName("gameVersion")] public required string GameVersion;
    [JsonPropertyName("fileId")] public int FileId;
    [JsonPropertyName("filename")] public required string FileName;

    [JsonPropertyName("releaseType")] public int ReleaseType;

    [JsonPropertyName("gameVersionTypeId")]
    public int? GameVersionTypeId;

    [JsonPropertyName("modLoader")] public int ModLoader;

}