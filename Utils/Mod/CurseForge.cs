using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Mod;

public class CurseForgeMod
{
    private static string CurseForgeBaseAPI = "https://api.curseforge.com/v1";

    public static async Task<JsonNode?> GetModInfomationByHash(List<string> modHash)
    {
        HttpResponseMessage result = await Network.GetResponse(
            CurseForgeBaseAPI + "/fingerprints/432",
            HttpMethod.Post,
            new Dictionary<string, string>()
            {
                ["Content-Type"] = "application/json"
            },
            new JsonObject()
            {
                ["fingerprints"] = JsonSerializer.Serialize(modHash)
            }.ToJsonString());
    


        return JsonNode.Parse(await result.Content.ReadAsStringAsync());
    }
    
}