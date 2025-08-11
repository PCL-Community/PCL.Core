using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFingerprintData
{
    [JsonPropertyName("isCacheBuilt")] public bool IsCacheBuilt;
    [JsonPropertyName("exactMatches")] public required CurseForgeMatches ExactMatches;

    [JsonPropertyName("exactFingerprints")]
    public required List<uint> ExactFingerprints;

    [JsonPropertyName("partialMatches")] public required CurseForgeMatches PartialMatches;
    
    [JsonPropertyName("partialMatchFingerprints")]
    public required CurseForgeMatchFingerprint PartialMatchFingerprints;

    [JsonPropertyName("installedFingerprints")]
    public required List<uint?> InstalledFingerprints;

    [JsonPropertyName("unmatchedFingerprints")]
    public required List<uint?> UnmatchedFingerprints;
}