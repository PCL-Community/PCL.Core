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
using PCL.Core.Utils.Exts;
using PCL.Core.Minecraft.Instance;

namespace PCL.Core.Minecraft.Instance.Clients;

public class MinecraftClient : IClient
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
    public static async Task<string> GetJsonAsync(string mcVersion, string? exceptHash = null)
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
    public static List<DownloadItem> AnalysisLibrary(JsonNode versionJson)
    {
        var list = new List<DownloadItem>();
        foreach (var library in versionJson["libraries"]!.AsArray())
        {
            var rules = library?["rules"];
            // skip check when rules is null
            if (rules is not null) foreach (var rule in rules.AsArray())
                {
                    // do nothing when allow/disallow (it skipped by continue)
                    switch (rule!["action"]!.ToString())
                    {
                        case "disallow":
                            var os = rule["os"];
                            var osName = os!["name"]?.ToString();
                            var arch = os!["arch"]?.ToString();
                            if (!string.IsNullOrEmpty(osName) &&
                                RuntimeInformation.IsOSPlatform(OSPlatform.Create(osName.ToUpper()))) continue;
                            var currentArchitecture = Architecture.X86;

                            if (!Enum.TryParse(arch!.Capitalize(), out currentArchitecture)) continue;
                            if (!string.IsNullOrEmpty(arch) &&
                                RuntimeInformation.OSArchitecture == currentArchitecture) continue;
                            break;
                        case "allow":
                        default:
                            break;
                    }
                }
            var artifact = library?["downloads"]?["artifact"];
            var classifiers = library?["downloads"]?["classifiers"];
            if (artifact is not null)
            {
                list.Add(new DownloadItem())
            }
            if (classifiers is not null)
            {
                // get key by os type
                var nativeKey = library?["natives"]?[Environment.OSVersion.Platform.ToString()]?.ToString();
                if (string.IsNullOrEmpty(nativeKey)) continue;
                if (nativeKey.Contains("arch"))
                    nativeKey = nativeKey.Replace("${arch}", $"{(RuntimeInformation.OSArchitecture == Architecture.X86 ? "86" : "64")}");

            }
        }
        return list;
    }
}