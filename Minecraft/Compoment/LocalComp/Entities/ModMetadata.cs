using System.Collections.Generic;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record ModMetadata
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Version { get; init; }

    public required string Id { get; init; }

    // NOTE: this have a problem: some mod's authors not use string array but object array
    public required List<string> Authors { get; init; }

    public required string Icon { get; init; }
    public string? Url { get; init; }
}
