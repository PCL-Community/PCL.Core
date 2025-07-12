using System;

namespace PCL.Core.Model.Mod.Curseforge;

[Serializable]
public record CurseforgeHashes(
    string value,
    int algo);