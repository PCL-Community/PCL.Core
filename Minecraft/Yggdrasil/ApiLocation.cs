using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Yggdrasil;

public static class ApiLocation
{
    public static async Task<string> GetApiLocation(string address)
    {
        if (!address.StartsWith("http")) address = $"https://{address}";
        using var response = (await HttpRequestBuilder.Create(address, HttpMethod.Head).Build()).GetResponse();
        response.Headers
            .TryGetValues("X-Authlib-Injector-Api-Location", out var apiAddresses);
        var currentApiAddr = new Uri(address);
        if (apiAddresses is null) return string.Empty;
        var apiAddr = apiAddresses.First() ?? "";
        if (string.IsNullOrEmpty(apiAddr)) return address;
        if (apiAddr.StartsWith(currentApiAddr.Scheme)) return apiAddr;
        // 不允许 HTTPS 降 HTTP
        if (apiAddr.StartsWith("http:") && currentApiAddr.Scheme == "https") return apiAddr.Replace("http","https");
        return (new Uri(currentApiAddr, apiAddr)).ToString();   
        
    }
}