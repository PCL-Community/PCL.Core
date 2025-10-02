using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record ModMetadata
{
    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("version")] public string? Version { get; set; }

    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("authors")] // NOTE: this have a problem: some mod not use string array but object array
    public required List<string> Authors { get; set; }

    [JsonPropertyName("icon")] public required string Icon { get; set; }
}