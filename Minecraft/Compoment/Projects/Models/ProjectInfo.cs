using System;
using System.Collections.Generic;
using PCL.Core.Minecraft.Compoment.Projects.Enums;

namespace PCL.Core.Minecraft.Compoment.Projects.Models;

public record ProjectInfo
{
    public bool FromCurseForge { get; init; }
    public CompType Type { get; init; }
    public required string Slug { get; init; }
    public required string Id { get; init; }
    public IReadOnlyList<int> CurseForgeFileIds { get; init; } = [];
    public required string RawName { get; init; }
    public required string Description { get; init; }
    public required string Website { get; init; }
    public DateTime? LastUpdate { get; init; }
    public int DownloadCount { get; init; }
    public IReadOnlyList<LoaderType> ModLoaders { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? LogoUrl { get; init; }
    public IReadOnlyList<int> GameVersions { get; init; } = [];
}