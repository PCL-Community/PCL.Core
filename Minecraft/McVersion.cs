using PCL.Core.Minecraft.Instance.Utils;
using PCL.Core.Utils.Exts;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft;

public enum McVersionType
{
    Snapshot,
    Release,
    Fool,
    OldAlpha,
    OldBeta,
    Unknown
}

public class McVersion
{
#pragma warning disable CA2211
    public static JsonNode Manifest = RefreshManifest();
#pragma warning restore
    public McVersion(string id)
    {
        Id = id;
        JsonUrl = "";
        var manifest = Manifest;
        if (manifest["versions"] is not null)
        {
            JsonArray versions = manifest["versions"] as JsonArray ?? [];
            JsonNode? current = null;
            foreach (var version in versions)
            {
                if (version != null && (version["id"]?.ToString() ?? "") == id)
                {
                    current = version;
                    break;
                }
            }

            if (current is null)
                throw new FormatException();

            JsonUrl = current["url"]?.ToString() ?? "";

            switch (current["type"]?.ToString() ?? "")
            {
                case "snapshot":
                    VersionType = McVersionType.Snapshot;
                    var idLower = id.ToLower();
                    if (idLower.StartsWith("1.")
                        && !idLower.Contains("rc")
                        && !idLower.Contains("combat")
                        && !idLower.Contains("experimental")
                        && !idLower.Contains("pre"))
                        VersionType = McVersionType.Release;
                    switch (idLower)
                    {
                        case "20w14infinite":
                        case "20w14∞":
                            Id = "20w14∞";
                            VersionType = McVersionType.Fool;
                            break;
                        case "3d shareware v1.34":
                        case "1.rv-pre1":
                        case "15w14a":
                        case "2.0":
                        case "22w13oneblockatatime":
                        case "23w13a_or_b":
                        case "24w14potato":
                            VersionType = McVersionType.Fool;
                            break;
                        default:
                            var releaseDate = current["releaseTime"]?.GetValue<DateTime>().ToUniversalTime().AddHours(2);
                            if (releaseDate is { Month: 4, Day: 1 })
                            {
                                VersionType = McVersionType.Fool;
                                break;
                            }
                            VersionType = McVersionType.Snapshot;
                            break;
                    }
                    break;
                case "release":
                    VersionType = McVersionType.Release;
                    break;
                case "old_alpha":
                    VersionType = McVersionType.OldAlpha;
                    break;
                case "old_beta":
                    VersionType = McVersionType.OldBeta;
                    break;
                default:
                    VersionType = McVersionType.Unknown;
                    break;
            }
        }
        
        return;
        
       
    }
    public string Id { get; }
    public McVersionType VersionType { get; }
    public string JsonUrl { get; }
    public DateTime Time { get; }
    public DateTime ReleaseTime { get; }
    
    public static JsonNode RefreshManifest()
    {
        var client = new HttpClient();
        var getTask = client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest.json");
        getTask.Wait();
        var readTask = getTask.Result.Content.ReadAsStringAsync();
        readTask.Wait();
        return Manifest = JsonNode.Parse(readTask.Result ?? "") ?? new JsonObject();
    }
}

