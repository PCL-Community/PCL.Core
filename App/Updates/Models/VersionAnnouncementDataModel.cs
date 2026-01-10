using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.App.Updates.Models;

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