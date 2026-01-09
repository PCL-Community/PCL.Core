using System;
using System.Text.Json.Serialization;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Models;

public class VersionData
{
    public VersionData(VersionDataModel self, string source)
    {
        VersionName = self.Version.Name;
        VersionCode = self.Version.Code;
        Sha256 = self.Sha256;
        ChangeLog = self.ChangeLog;
        Source = source;
        IsAvailable = 
            SemVer.Parse(VersionName) > SemVer.Parse(Basics.VersionName) && 
            VersionCode > Basics.VersionCode;
    }

    public bool IsAvailable { get; }
    
    public required string VersionName { get; init; }
    
    public required int VersionCode { get; init; }
    
    public required string Sha256 { get; init; }
    
    public required string ChangeLog { get; init; }
    
    public required string Source { get; init; }
}

public class VersionAssetsDataModel
{
    [JsonPropertyName("assets")] public required VersionDataModel[] Assets { get; init; }
}

public class VersionDataModel
{
    [JsonPropertyName("version")] public required VersionInfoDataModel Version { get; init; }
    
    [JsonPropertyName("sha256")] public required string Sha256 { get; init; }
    
    [JsonPropertyName("changelog")] public required string ChangeLog { get; init; }
    
    [JsonPropertyName("patches")] public required string[] Patches { get; init; }
}

public class VersionInfoDataModel
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    
    [JsonPropertyName("code")] public required int Code { get; init; }
}