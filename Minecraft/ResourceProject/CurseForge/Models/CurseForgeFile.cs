using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeFile
{
    [JsonPropertyName("file")] public int Id;
    [JsonPropertyName("gameId")] public int GameId;
    [JsonPropertyName("modId")] public int ModId;
    [JsonPropertyName("isAvailable")] public bool IsAvailable;
    [JsonPropertyName("displayName")] public required string DisplayName;
    [JsonPropertyName("fileName")] public required string FileName;
    [JsonPropertyName("releaseType")] public int ReleaseType;
    [JsonPropertyName("fileStatus")] public int FileStatus;
    [JsonPropertyName("hashes")] public required List<CurseForgeHashes> Hashes;
    [JsonPropertyName("fileDate")] public required string FileDate;
    [JsonPropertyName("fileLength")] public long FileLength;
    [JsonPropertyName("downloadCount")] public int DownloadCount;
    [JsonPropertyName("downloadUrl")] public required string DownloadUrl;
    [JsonPropertyName("gameVersions")] public required List<string?> GameVersions;
    [JsonPropertyName("sortableGameVersions")]
    public required List<CurseForgeSortableGameVersion?> SortableGameVersions;

    [JsonPropertyName("dependencies")] public required List<CurseForgeDependency?> Dependencies;
    [JsonPropertyName("alternateFileId")] public int AlternateFileId;
    [JsonPropertyName("isServerPack")] public bool IsServerPack;
    [JsonPropertyName("fileFingerprint")] public int FileFingerprint;
    [JsonPropertyName("modules")] public required List<CurseForgeModules?> Modules;
};