using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFile
{
    [JsonPropertyName("file")] public uint Id;
    [JsonPropertyName("gameId")] public uint GameId;
    [JsonPropertyName("modId")] public uint ModId;
    [JsonPropertyName("isAvailable")] public bool IsAvailable;
    [JsonPropertyName("displayName")] public required string DisplayName;
    [JsonPropertyName("fileName")] public required string FileName;
    [JsonPropertyName("releaseType")] public uint ReleaseType;
    [JsonPropertyName("fileStatus")] public uint FileStatus;
    [JsonPropertyName("hashes")] public required List<CurseForgeHashes> Hashes;
    [JsonPropertyName("fileDate")] public required string FileDate;
    [JsonPropertyName("fileLength")] public long FileLength;
    [JsonPropertyName("downloadCount")] public uint DownloadCount;
    [JsonPropertyName("downloadUrl")] public required string DownloadUrl;
    [JsonPropertyName("gameVersions")] public required List<string?> GameVersions;
    [JsonPropertyName("sortableGameVersions")]
    public required List<CurseForgeSortableGameVersion?> SortableGameVersions;

    [JsonPropertyName("dependencies")] public required List<CurseForgeDependency?> Dependencies;
    [JsonPropertyName("alternateFileId")] public uint AlternateFileId;
    [JsonPropertyName("isServerPack")] public bool IsServerPack;
    [JsonPropertyName("fileFingerprint")] public uint FileFingerprint;
    [JsonPropertyName("modules")] public required List<CurseForgeModules?> Modules;
};