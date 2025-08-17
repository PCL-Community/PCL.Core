using System.Text.Json.Serialization;

namespace PCL.Core.Link.EasyTier.Models;

public record EasyTierPeerInfo
{
    [JsonPropertyName("hostname")] public required string Hostname { get; set; }
    [JsonPropertyName("ipv4")] public required string Ipv4 { get; set; }
    [JsonPropertyName("cost")] public required string Cost { get; set; }
    [JsonPropertyName("lat_ms")] public required string Ping { get; set; }
    [JsonPropertyName("loss_rate")] public required string Loss { get; set; }
    [JsonPropertyName("nat_type")] public required string NatType { get; set; }
    [JsonPropertyName("version")] public required string ETVersion { get; set; }
}
