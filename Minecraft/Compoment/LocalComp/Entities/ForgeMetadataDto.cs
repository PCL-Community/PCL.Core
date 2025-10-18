using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record ForgeMetadataDto
{
    [DataMember(Name = "mods")] public List<ModInfoDto> Mods { get; init; } = [];

    [DataMember(Name = "dependencies")]
    public Dictionary<string, List<DependencyInfoDto>> Dependencies { get; init; } = [];
}

public record ModInfoDto
{
    [DataMember(Name = "modId")] public required string ModId { get; init; }
    [DataMember(Name = "version")] public required string Version { get; init; }
    [DataMember(Name = "dispalyName")] public required string DisplayName { get; init; }
    [DataMember(Name = "displayUrl")] public string? Url { get; init; }
    [DataMember(Name = "logoFile")] public required string LofoFile { get; init; }
    [DataMember(Name = "description")] public required string Description { get; init; }
    [DataMember(Name = "authors")] public required string Authors { get; init; }
}

public record DependencyInfoDto
{
    [DataMember(Name = "modId")] public required string ModId { get; init; }

    [DataMember(Name = "mandatory")] public bool Mandatory { get; init; }

    [DataMember(Name = "versionRange")] public required string VersionRange { get; init; }

    [DataMember(Name = "ordering")] public string Ordering { get; init; } = "NONE";

    [DataMember(Name = "side")] public string Side { get; init; } = "BOTH";
}