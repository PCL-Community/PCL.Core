using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PCL.Core.Model.Mod;
using PCL.Core.Service;
using PCL.Core.Utils.Minecraft;

namespace PCL.Core.Utils.ModPlatform;

public class ModrinthPlatformService : IModPlatformService
{
    private const string BaseUrl = "https://api.modrinth.com";
    
    public async Task<List<ModSearchResult>> Search(ModSearch query)
    {
        var queryUri = $"{BaseUrl}/search?";
        queryUri += $"query={query.keyWord}";
        var facts = new ModrinthFactsBuilder()
            .EqualCategories(query.categories ?? Enumerable.Empty<string>())
            .EqualCategories(query.modLoaders.Select(ProjectType2String) ?? Enumerable.Empty<string>())
            .EqualGameVersion(query.gameVersions ?? Enumerable.Empty<string>())
            .EqualProjectType(query.ModProjectType);
        
        using var request = new HttpRequestMessage(HttpMethod.Get,);
        using var ret = await HttpClientService.GetClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        ret.EnsureSuccessStatusCode();
        var content = await ret.Content.ReadAsStringAsync();
        
    }

    private static Dictionary<ModProjectType, string> _projectTypeStringMap = new()
    {
        [ModProjectType.Mod] = "mod",
        [ModProjectType.Modpack] = "modpack",
        [ModProjectType.ResourcePack] = "resourcepack",
        [ModProjectType.Shader] = "shader"
    };

    public static string ProjectType2String(ModProjectType projectType) => _projectTypeStringMap.TryGetValue(projectType, out var value) ? value : string.Empty;
    public static ModProjectType String2ProjectType(string projectType) => _projectTypeStringMap.FirstOrDefault(x => x.Value == projectType).Key;
    
    private static Dictionary<ModLoaders, string> _loaderStringMap = new()
    {
        [modl]
    }
}