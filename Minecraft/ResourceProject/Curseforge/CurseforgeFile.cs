using System;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeFile(
    int Id,
    int GameId,
    int ModId,
    bool IsAvailable,
    string DisplayName,
    string FileName,
    int ReleaseType,
    int FileStatus,
    CurseforgeHashes Hashes);