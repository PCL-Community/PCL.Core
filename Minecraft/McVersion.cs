using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;

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

[LifecycleService(LifecycleState.Loaded, Priority = 100)]
[LifecycleScope("mc_version", "MC 版本")]
public sealed partial class McVersion
{
#pragma warning disable CS8618
    // Expected CS8618
    // Initialized when loaded
    public static JsonNode Manifest { get; set; }
#pragma warning restore

    [LifecycleStart]
    private static async Task _Start()
    {
        Manifest = await RefreshManifest();
    }

    [LifecycleStop]
    private static void _Stop() { }

    public McVersion(string id)
    {
        Id = id;
        JsonUrl = "";
        var manifest = Manifest;
        if (manifest["versions"] is not null)
        {
            var versions = manifest["versions"] as JsonArray ?? [];
            var current = versions.OfType<JsonNode>().FirstOrDefault(version => (version["id"]?.ToString() ?? "") == id);

            if (current is null)
                throw new FormatException();

            JsonUrl = current["url"]?.ToString() ?? "";

            var time = current["time"]?.GetValue<DateTime>().ToUniversalTime();
            var releaseTime = current["releaseTime"]?.GetValue<DateTime>().ToUniversalTime();

            if (time is null || releaseTime is null)
                throw new FormatException();

            Time = (DateTime)time;
            ReleaseTime = (DateTime)releaseTime;

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
                            var releaseDate = current["releaseTime"]?.GetValue<DateTime>().ToUniversalTime()
                                .AddHours(2);
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
        else
            throw new FormatException();

        return;
    }
    public string Id { get; }
    public McVersionType VersionType { get; }
    public string JsonUrl { get; }
    public DateTime Time { get; }
    public DateTime ReleaseTime { get; }

    public static bool operator <(McVersion first, McVersion second)
    {
        return first.ReleaseTime < second.ReleaseTime;
    } 
    public static bool operator <=(McVersion first, McVersion second)
    {
        return first.ReleaseTime <= second.ReleaseTime;
    }

    public static bool operator >(McVersion first, McVersion second)
    {
        return first.ReleaseTime > second.ReleaseTime;
    }
    public static bool operator >=(McVersion first, McVersion second)
    {
        return first.ReleaseTime >= second.ReleaseTime;
    }

    public static bool operator ==(McVersion first, McVersion second)
    {
        return first.Id == second.Id;
    }
    public static bool operator !=(McVersion first, McVersion second)
    {
        return first.Id != second.Id;
    }

    public async static Task<JsonNode> RefreshManifest()
    {
        var client = new HttpClient();
        var getTask = await client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest.json");
        var readTask = await getTask.Content.ReadAsStringAsync();
        return Manifest = JsonNode.Parse(readTask) ?? new JsonObject();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (ReferenceEquals(obj, null))
            return false;

        try
        {
            var converted = (McVersion)obj;
            return Id == converted.Id;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, (int)VersionType);
    }
}

