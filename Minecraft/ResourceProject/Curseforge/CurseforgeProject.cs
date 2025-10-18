using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record class CurseforgeProject(
    int Id,
    int GameId,
    string Name,
    string Slug,
    CurseforgeLinks Links,
    string Summary,
    int Status,
    int DownloadCount,
    bool IsFeatured,
    int PrimaryCategoryId,
    List<CurseforgeCategories> Categories,
    int ClassId,
    List<CurseforgeAuthors> Authors,
    CurseforgePictures Logo,
    List<CurseforgePictures> Screenshots,
    int MainFileId,
    object LatestFiles);