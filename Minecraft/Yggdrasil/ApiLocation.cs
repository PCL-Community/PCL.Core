using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Yggdrasil;

public static class ApiLocation
{
    public static async Task<string> TryRequest(string address)
    {
        var originAddr = address.StartsWith("http") ? address : $"https://{address}";
        var originUri = new Uri(originAddr);
        using var response = (await HttpRequestBuilder.Create(originAddr, HttpMethod.Head).Build()).GetResponse();
        response.Headers.TryGetValues("X-Authlib-Injector-Api-Location", out var responses);
        var resultAddr = responses?.First();
        if (string.IsNullOrEmpty(resultAddr)) return originAddr;
        if (resultAddr.StartsWith(originUri.Scheme)) return resultAddr;
        // 不允许 HTTPS 降 HTTP
        if (resultAddr.StartsWith("http:") && originUri.Scheme == "https") return resultAddr.Replace("http","https");
        return (new Uri(originUri, resultAddr)).ToString();   
    }
}
