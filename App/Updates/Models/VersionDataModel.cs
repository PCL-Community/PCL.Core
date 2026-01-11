using System;
using System.Text.Json.Serialization;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Models;

public sealed record VersionAssetsDataModel
{
    [JsonPropertyName("assets")] public required VersionDataModel[] Assets { get; init; }
}

public sealed record VersionDataModel
{
    public bool IsAvailable => Version.Code > Basics.VersionCode &&
                               SemVer.Parse(Version.Name) > SemVer.Parse(Basics.VersionName);
    
    [JsonPropertyName("version")] public required VersionInfoDataModel Version { get; init; }
    
    [JsonPropertyName("sha256")] public required string Sha256 { get; init; }
    
    [JsonPropertyName("changelog")] public required string ChangeLog { get; init; }
    
    [JsonPropertyName("patches")] public required string[] Patches { get; init; }
    
    [JsonPropertyName("downloads")] public required string[] Downloads { get; init; }
}

public sealed record VersionInfoDataModel
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    
    [JsonPropertyName("code")] public required int Code { get; init; }
}