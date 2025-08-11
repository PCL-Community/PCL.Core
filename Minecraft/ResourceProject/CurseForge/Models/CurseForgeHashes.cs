using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeHashes
{
    /// <summary>
    /// Hash
    /// </summary>
    [JsonPropertyName("value")] public required string Value;
    /// <summary>
    /// 算法
    /// </summary>
    [JsonPropertyName("algo")] public uint Algorithm;
}