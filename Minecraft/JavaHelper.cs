using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.Net;

namespace PCL.Core.Minecraft;

public static class JavaHelper
{
    private static string[] _GetJavaIndexUrl() => [
        "https://piston-meta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json",
        "https://bmclapi2.bangbang93.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json"
        ];
    private static JsonNode? _JavaIndex;
    public static async Task UpdateJavaIndex()
    {
        foreach (var url in _GetJavaIndexUrl())
        {
            var result = await HttpRequestBuilder.Create(url, HttpMethod.Get).SendAsync(false);
            if (!result.IsSuccess) continue;
            _JavaIndex = await result.AsJsonAsync<JsonNode>();
        }
        throw new HttpRequestException("Failed to download version json:All of source unavailable");
    }
    public static string GetIndexUrlByVersion(int mojarVersaion) {
        foreach (var kvp in _JavaIndex!.AsObject())
        {
            if (kvp.Key == "gamecore") continue;
            var os = kvp.Key;
            if (os.Contains("-")) os = os.Split("-")[0];
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.ToUpper()))) continue;
            foreach (var javas in kvp.Value!.AsObject())
            {
                
            }
        }
        return string.Empty;
    }
    public static string GetIndexUrlByName(string name) {
        return string.Empty;
    }
}