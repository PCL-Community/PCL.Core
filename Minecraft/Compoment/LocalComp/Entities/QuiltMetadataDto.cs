using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record QuiltMetadataDto
{
    [JsonPropertyName("quilt_loader")] public required QuiltLoaderDto Loader { get; init; }
}

public record QuiltLoaderDto
{
    [JsonPropertyName("metadata")] public required QuiltLoaderMetadataDto Metadata { get; init; }
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("version")] public string? Version { get; init; }
    [JsonPropertyName("icon")] public string? Icon { get; init; }
}

public record QuiltLoaderMetadataDto
{
    [JsonPropertyName("name")] public required string Name { get; init; }

    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("contact")] public QuiltMetadataContactDto? Contact { get; init; }
}

public record QuiltMetadataContactDto
{
    [JsonPropertyName("homepage")] public string? Homepage { get; init; }
}