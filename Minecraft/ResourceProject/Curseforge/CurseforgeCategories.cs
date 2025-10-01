using System;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeCategories(
    int Id,
    int GameId,
    string Name,
    string Slug,
    string Url,
    string IconUrl,
    string DateModified,
    bool IsClass,
    int ClassId,
    int ParentCategoryId,
    int DisplayIndex);