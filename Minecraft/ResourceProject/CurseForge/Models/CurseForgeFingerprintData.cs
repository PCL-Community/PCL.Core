using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFingerprintData
{
    [JsonPropertyName("isCacheBuilt")] public bool IsCacheBuilt;
    [JsonPropertyName("exactMatches")] public required List<CurseForgeMatches> ExactMatches;

    [JsonPropertyName("exactFingerprints")]
    public required List<int> ExactFingerprints;

    [JsonPropertyName("partialMatches")] public required List<CurseForgeMatches?> PartialMatches;
    
    [JsonPropertyName("partialMatchFingerprints")]
    public required CurseForgeMatchFingerprint PartialMatchFingerprints;

    [JsonPropertyName("installedFingerprints")]
    public required List<int?> InstalledFingerprints;

    [JsonPropertyName("unmatchedFingerprints")]
    public required List<int?> UnmatchedFingerprints;
}