using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeMatches
{
    [JsonPropertyName("id")] public uint Id;
    [JsonPropertyName("file")] public required CurseForgeFile File;
    [JsonPropertyName("latestFiles")] public required List<CurseForgeFile?> LatestFiles;
}