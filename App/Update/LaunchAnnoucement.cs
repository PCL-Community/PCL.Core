using System.Text.Json.Serialization;

namespace PCL.Core.App.Update;

public class LaunchAnnoucement
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("detail")]
    public required string Detail { get; set; }
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("date")]
    public required string Date { get; set; }
    [JsonPropertyName("btn1")]
    public LaunchAnnouncementBtn? Btn1 { get; set; }
    [JsonPropertyName("btn2")]
    public LaunchAnnouncementBtn? Btn2 { get; set; }
}

public class LaunchAnnouncementBtn
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
    [JsonPropertyName("command")]
    public required string Command { get; set; }
    [JsonPropertyName("command_paramter")]
    public required string CommandParameter { get; set; }
}