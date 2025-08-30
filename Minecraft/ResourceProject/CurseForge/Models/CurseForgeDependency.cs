using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeDependency
{
    [JsonPropertyName("modId")] public int ModId;
    [JsonPropertyName("relationTypeType")] public int RelationType;
};