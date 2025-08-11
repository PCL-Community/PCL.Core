using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeDependency
{
    [JsonPropertyName("modId")] public uint ModId;
    [JsonPropertyName("relationTypeType")] public uint RelationType;
};