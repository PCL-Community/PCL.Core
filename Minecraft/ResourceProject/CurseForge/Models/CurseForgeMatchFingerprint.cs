using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeMatchFingerprint
{
    [JsonPropertyName("property1")] public required List<int?> Property1;
    [JsonPropertyName("property2")] public required List<int?> Property2;
};