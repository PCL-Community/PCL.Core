using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFingerprints
{
    [JsonPropertyName("data")] public required CurseForgeFingerprintData Data;
};