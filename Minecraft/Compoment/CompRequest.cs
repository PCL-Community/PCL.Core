using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Compoment.Projects;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Compoment;

public static class CompRequest
{
    public static bool IsFromCurseForge(string id)
        => int.TryParse(id, out _);

    public static async Task<ImmutableList<ProjectInfo>> GetProjFromModrinthAsync(IReadOnlyList<string> ids)
    {
        var reqStr = await ModApiMirrorSourceReq.RequestAsync(
                $"https://api.modrinth.com/v2/projects?ids=[\"{string.Join("\",\"", ids)}\"]")
            .ConfigureAwait(false);
        using var document = JsonDocument.Parse(reqStr);
        var rootEle = document.RootElement.EnumerateArray();
        return rootEle.Select(ele => ProjectFactory.Create(ele.GetRawText())).ToImmutableList();
    }

    public static async Task<ImmutableList<ProjectInfo>> GetProjFromCurseForgeAsync(IReadOnlyList<string> ids)
    {
        var reqStr = await ModApiMirrorSourceReq.RequestAsync("https://api.curseforge.com/v1/mods",
            $"{{\"modIds\":[{string.Join(',', ids)}]}}", HttpMethod.Post).ConfigureAwait(false);
        using var document = JsonDocument.Parse(reqStr);
        var dataSeg = document.RootElement.GetProperty("data").EnumerateArray();
        return dataSeg.Select(seg => ProjectFactory.Create(seg.GetRawText())).ToImmutableList();
    }

    public static async Task<List<ProjectInfo>> GetProjectsByIdAsync(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        List<string> modrinthIds = [];
        List<string> curseforgeIds = [];
        foreach (var id in ids)
        {
            if (IsFromCurseForge(id))
            {
                curseforgeIds.Add(id);
            }
            else
            {
                modrinthIds.Add(id);
            }
        }

        var modrinthProjs = await GetProjFromModrinthAsync(modrinthIds).ConfigureAwait(false);
        var curseforgeProjs = await GetProjFromCurseForgeAsync(curseforgeIds).ConfigureAwait(false);


        return [..modrinthProjs, ..curseforgeProjs];
    }
}