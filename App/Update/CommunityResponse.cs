using System.Text.Json.Serialization;

namespace PCL.Core.App.Update;

public class CommunityResponse
{
    [JsonPropertyName("assets")]
    public required LaunchAssetItem[] Assets { get; set; }
}