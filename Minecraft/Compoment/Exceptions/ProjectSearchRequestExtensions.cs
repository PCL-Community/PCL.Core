using System.Collections.Generic;
using System.Net;
using System.Text;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Minecraft.Compoment.Projects.Enums;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Compoment.Exceptions;

public static class ProjectSearchRequestExtensions
{
    public static string GetCuresForgeAddress(this ProjectSearchRequest req)
    {
        if (req.Source.HasFlag(SourceType.CurseForge))
        {
            return string.Empty;
        }

        if (req.Tag.StartsWith('/'))
        {
            req.Storage.CurseForgeTotal = 0;
        }

        if (req.Storage.CurseForgeTotal > -1 && req.Storage.CurseForgeTotal <= req.Storage.CurseForgeOffset)
        {
            return string.Empty;
        }

        var address =
            new StringBuilder(
                "https://api.curseforge.com/v1/mods/search?gameId=432&sortField=2&sortOrder=desc&pageSize=40"); // NOTE: the pageSize=40 is a const val

        switch (req.Type)
        {
            case CompType.Mod:
                address.Append("&classId=6");
                break;
            case CompType.ModPack:
                address.Append("&classId=4471");
                break;
            case CompType.ResourcePack:
                address.Append("&classId=12");
                break;
            case CompType.Shader:
                address.Append("&classId=6552");
                break;
            case CompType.DataPack:
                address.Append("&classId=6945");
                break;
            case CompType.World:
                address.Append("&classId=17");
                break;
        }

        var categoryId = string.IsNullOrEmpty(req.Tag) ? "0" : req.Tag.BeforeFirst('/');
        address.Append("&categoryId=").Append(categoryId);

        if (req.ModLoader is not LoaderType.Any)
        {
            address.Append("&modLoaderType=").Append((int)req.ModLoader);
        }

        if (!string.IsNullOrEmpty(req.GameVersion))
        {
            address.Append("&gameVersion=").Append(req.GameVersion);
        }

        if (!string.IsNullOrEmpty(req.SearchText))
        {
            address.Append("&searchText=").Append(WebUtility.UrlEncode(req.SearchText));
        }

        if (req.Storage.CurseForgeOffset > 0)
        {
            address.Append("&index=").Append(req.Storage.CurseForgeOffset);
        }

        switch (req.Sort)
        {
            case SortType.Relevance:
                address.Append("&sortField=4&sortOrder=desc");
                break;
            case SortType.Downloads:
                address.Append("&sortField=6&sortOrder=desc");
                break;
            case SortType.Follows:
                address.Append("&sortField=2&sortOrder=desc");
                break;
            case SortType.Newest:
                address.Append("&sortField=11&sortOrder=desc");
                break;
            case SortType.Updated:
                address.Append("&sortField=3&sortOrder=desc");
                break;
        }

        return address.ToString();
    }

    public static string GetModrinthAddress(this ProjectSearchRequest req)
    {
        if (!req.Source.HasFlag(SourceType.Modrinth))
        {
            return string.Empty;
        }

        if (req.Tag.EndsWith('/'))
        {
            req.Storage.ModrinthTotal = 0;
        }

        if (req.Storage.ModrinthTotal > -1 &&
            req.Storage.ModrinthTotal <= req.Storage.ModrinthOffset)
        {
            return string.Empty;
        }

        var address =
            new StringBuilder("https://api.modrinth.com/v2/search?limit=40"); // NOTE: the limit=40 is a const val

        switch (req.Sort)
        {
            case SortType.Relevance:
                address.Append("&index=relevance");
                break;
            case SortType.Downloads:
                address.Append("&index=downloads");
                break;
            case SortType.Follows:
                address.Append("&index=follows");
                break;
            case SortType.Newest:
                address.Append("&index=newest");
                break;
            case SortType.Updated:
                address.Append("&index=updated");
                break;
        }

        if (!string.IsNullOrEmpty(req.SearchText))
        {
            address.Append("&query=").Append(WebUtility.UrlEncode(req.SearchText));
        }

        if (req.Storage.ModrinthOffset > 0)
        {
            address.Append("&offset=").Append(req.Storage.ModrinthOffset);
        }

        var facets = new List<string> { $"[\"project_type:{req.Type.ToString().ToLowerInvariant()}\"]" };

        if (!string.IsNullOrEmpty(req.Tag))
        {
            facets.Add($"[\"categories:'{req.Tag.AfterLast('/')}'\"]");
        }

        if (req.ModLoader is not LoaderType.Any)
        {
            facets.Add($"[\"categories:'{req.ModLoader.ToString().ToLowerInvariant()}'\"]");
        }

        if (!string.IsNullOrEmpty(req.GameVersion))
        {
            facets.Add($"[\"versions:'{req.GameVersion}'\"]");
        }

        address.Append("&facets=[").Append(string.Join(',', facets)).Append(']');

        return address.ToString();
    }
}