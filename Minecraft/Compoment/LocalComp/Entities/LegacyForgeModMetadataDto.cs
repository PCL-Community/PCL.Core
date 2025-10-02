using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record LegacyForgeModMetadataDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("version")]
    string? Version,
    [property: JsonPropertyName("modid")] string Id,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("authorList")]
    List<string> Authors,
    [property: JsonPropertyName("logoFile")]
    string? LogoFile,
    [property: JsonPropertyName("requireMods")]
    List<string>? RequireMods,
    [property: JsonPropertyName("dependencies")]
    List<string>? Dependencies);