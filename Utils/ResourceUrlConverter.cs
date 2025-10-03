using System.Collections.Generic;
using PCL.Core.App;

namespace PCL.Core.Utils;

public static class ResourceUrlConverter
{
    public static string ModApiToMirror(string originalUrl) =>
        originalUrl.Replace("https://api.modrinth.com", "https://mod.mcimirror.top/modrinth")
            .Replace("https://api.curseforge.com", "https://mod.mcimirror.top/curseforge");

    public static IReadOnlyList<string> ModDownloadToMirror(string originalUrl)
    {
        var currentSetting = Config.Tool.Download.CompSourceSolution;
        var mirrorUrl = originalUrl.Replace("https://cdn.modrinth.com", "https://mod.mcimirror.top")
            .Replace("https://edge.forgecdn.net", "https://mod.mcimirror.top");

        IReadOnlyList<string> result = currentSetting switch
        {
            0 => [mirrorUrl, mirrorUrl],
            1 => [originalUrl, mirrorUrl],
            2 => [originalUrl, originalUrl],
            _ => [originalUrl]
        };

        if (result.Count == 1)
        {
            // TODO: i dont know how to reset
            //Config.Tool.Download.Reset();
        }

        return result;
    }
}