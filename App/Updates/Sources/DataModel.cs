using System.Text.Json.Serialization;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Sources;

public sealed record VersionInfoData
{
    [JsonPropertyName("name")] public required string Name { get; init; }
    
    [JsonPropertyName("code")] public required int Code { get; init; }
}

public sealed record VersionData
{
    public bool IsAvailable => Version.Code > Basics.VersionCode &&
                               SemVer.Parse(Version.Name) > SemVer.Parse(Basics.VersionName);
    [JsonPropertyName("version")] public required VersionInfoData Version { get; init; }
    
    [JsonPropertyName("sha256")] public required string Sha256 { get; init; }
    
    [JsonPropertyName("changelog")] public required string ChangeLog { get; init; }
    
    [JsonPropertyName("patches")] public required string[] Patches { get; init; }
    
    [JsonPropertyName("downloads")] public required string[] Downloads { get; init; }
}

public record VersionAnnouncementDataModel
{
    [JsonPropertyName("content")] public required VersionAnnouncementContentModel[] Contents { get; init; }
};

public record VersionAnnouncementContentModel
{
    [JsonPropertyName("title")] public required string Title { get; init; }
    
    [JsonPropertyName("detail")] public required string Detail { get; init; }
    
    [JsonPropertyName("id")] public required string Id { get; init; }
    
    [JsonPropertyName("date")] public required string Date { get; init; }
    
    [JsonPropertyName("btn1")] public required AnnouncementBtnInfoModel? Btn1 { get; init; }
    
    [JsonPropertyName("btn2")] public required AnnouncementBtnInfoModel? Btn2 { get; init; }
}

public record AnnouncementBtnInfoModel
{
    [JsonPropertyName("text")] public required string Text { get; init; }
    
    [JsonPropertyName("command")] public required string Command { get; init; }
    
    [JsonPropertyName("command_paramter")] public required string CommandParameter { get; init; }
}

