using System.Text.Json.Serialization;

namespace PCL.Core.App.Update;

public class MirrorChyanResponse
{
    [JsonPropertyName("code")]
    public required int Code { get; set; }
    [JsonPropertyName("msg")]
    public required string Message { get; set; }
    [JsonPropertyName("data")]
    public MirrorChyanData? Data { get; set; }

    public class MirrorChyanData
    {
        [JsonPropertyName("version_name")]
        public required string VersionName { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("release_note")]
        public required string ReleaseNote { get; set; }
    }
}