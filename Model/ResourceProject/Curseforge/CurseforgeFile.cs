﻿using System;

namespace PCL.Core.Model.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeFile(
    int id,
    int gameId,
    int modId,
    bool isAvailable,
    string displayName,
    string fileName,
    int releaseType,
    int fileStatus,
    CurseforgeHashes hashes);