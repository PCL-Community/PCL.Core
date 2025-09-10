using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Net.Http;
using PCL.Core.Net;
using PCL.Core.App;
using PCL.Core.Logging;
using System.Linq;
using System.Data;
using PCL.Core.Utils.Hash;
using System.Collections.Generic;
using System.Management;
using System;
using System.Runtime.InteropServices;

namespace PCL.Core.Minecraft.Instance;

public static class MinecraftClient
{
    public static JsonNode? VersionList;

    private const string Official = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
    private const string BmclApi = "https://bmclapi2.bangbang93.com/mc/game/version_manifest_v2.json";
    private static string[] _GetVersionSource() => Config.ToolConfigGroup.DownloadConfigGroup.VersionSourceSolution switch
    {
        0 => [Official, Official, BmclApi],
        1 => [Official, BmclApi, Official],
        2 => [BmclApi, BmclApi, Official]
    };
    private static string[] _GetFileSource(string uri)
    {
        var mirror = uri
        .Replace("piston-meta.mojang.com", "bmclapi2.bangbang93.com")
        .Replace("libraries.minecraft.net", "bmclapi2.bangbang93.com/maven")
        .Replace("pistom-data.mojang.com", "bmclapi2.bangbang93.com");
        return Config.ToolConfigGroup.DownloadConfigGroup.VersionSourceSolution switch
        {
            0 => [uri, uri, mirror],
            1 => [uri, mirror, uri],
            2 => [mirror, mirror, uri]
        };
    }

    public static async Task<JsonNode?> GetVersionInfoAsync(string mcVersion)
    {
        if (VersionList is null) await UpdateVersionIndexAsync();
        return VersionList!["versions"]?.AsArray().Where(value => value?["id"]?.ToString() == mcVersion).First();
    }
    public static async Task UpdateVersionIndexAsync()
    {
        foreach (var source in _GetVersionSource())
        {
            try
            {
                using var handler = await HttpRequestBuilder.Create(source, HttpMethod.Get).SendAsync(true);
                VersionList = await handler.AsJsonAsync<JsonNode>();
            }
            catch (HttpRequestException ex)
            {
                LogWrapper.Error(ex, "Minecraft", "Failed to get version list");
            }
        }
    }
    public static async Task<string> DownloadJsonAsync(string mcVersion, string? exceptHash = null)
    {
        var version = await GetVersionInfoAsync(mcVersion);
        if (version is null) throw new VersionNotFoundException($"Version not found: {mcVersion}");
        foreach (var source in _GetFileSource(version["url"]!.ToString()))
        {
            try
            {
                var response = await HttpRequestBuilder.Create(source, HttpMethod.Get).SendAsync(true);
                var content = await response.AsStringAsync();
                if (!string.IsNullOrEmpty(exceptHash))
                {
                    var hashResult = SHA1Provider.Instance.ComputeHash(content);
                    if (hashResult != exceptHash) continue;
                }
                return content;
            }
            catch (HttpRequestException ex)
            {
                LogWrapper.Error(ex, "Minecraft", "下载版本 Json 失败");
            }
        }
        throw new HttpRequestException("Failed to download version json:All of source unavailable");
    }
    public static async Task<List<JsonNode>> AnalysisLibrary(JsonNode versionJson)
    {
        var list = new List<JsonNode>();
        foreach (var library in versionJson["libraries"]!.AsArray())
        {
            var artifact = library?["downloads"]?["artifact"];
            var classifiers = library?["downloads"]?["classifiers"];
            if (artifact is not null) list.Add(artifact);
            if (classifiers is not null)
            {
                var rules = library?["rules"];
                var nativeKey = library?["natives"]?[Environment.OSVersion.Platform.ToString()]?.ToString();
                if (string.IsNullOrEmpty(nativeKey)) continue;
                foreach (var rule in rules!.AsArray())
                {
                    if (rule!["action"]!.ToString() == "disallow")
                    {
                        var os = rule["os"];
                        var osName = os!["name"]?.ToString();
                        var arch = os!["arch"]?.ToString();
                        if (!string.IsNullOrEmpty(osName) &&
                            RuntimeInformation.IsOSPlatform(OSPlatform.Create(osName.ToUpper()))) continue;
                        
                    }
                }
            }
        }
    }
    private 
}