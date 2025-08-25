using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeStringResponse
{
    [JsonPropertyName("data")] public required string Data;
}