using System;

namespace PCL.Core.Model.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeAuthors(
    int id,
    string name,
    string url);