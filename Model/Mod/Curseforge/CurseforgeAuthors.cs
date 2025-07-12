using System;

namespace PCL.Core.Model.Mod.Curseforge;

[Serializable]
public record CurseforgeAuthors(
    int id,
    string name,
    string url);