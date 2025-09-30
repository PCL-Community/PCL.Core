using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.LocalCompFiles;

public record ModMetadata(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("version")]
    string? Version,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("authors")] // NOTE: this have a problem: some mod not use string array but use object array
    List<string> Authors,
    [property: JsonPropertyName("icon")] string Icon);