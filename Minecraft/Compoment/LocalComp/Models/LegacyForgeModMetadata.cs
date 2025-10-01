using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record LegacyForgeModMetadata(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("version")]
    string? Version,
    [property: JsonPropertyName("modid")] string Id,
    [property: JsonPropertyName("authorList")]
    List<string> Authors,
    [property: JsonPropertyName("logoFile")]
    string? LogoFile);