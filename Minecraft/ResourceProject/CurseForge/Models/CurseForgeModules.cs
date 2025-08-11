using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeModules
{
    [JsonPropertyName("name")]public required string Name;
    [JsonPropertyName("fingerprint")] public int Fingerprint;
};