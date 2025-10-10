using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record FabricMetadataDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("version")] public string? Version { get; init; }

    [JsonPropertyName("id")] public required string Id { get; init; }

    [JsonPropertyName("authors")] // NOTE: this have a problem: some mod not use string array but object array
    public List<string>? Authors { get; init; }

    [JsonPropertyName("icon")] public required string Icon { get; init; }
    [JsonPropertyName("contact")] public FabricMetadataContactDto? Contact { get; init; }
}

public record FabricMetadataContactDto
{
    [JsonPropertyName("homepage")] public string? Homepage { get; init; }
}