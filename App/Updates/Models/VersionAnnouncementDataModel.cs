using System.Collections.Generic;

namespace PCL.Core.App.Updates.Models;

public record VersionAnnouncementDataModel(List<VersionAnnouncementContentModel> Contents);

public record VersionAnnouncementContentModel
{
    public required string Title { get; init; }
    public required string Detail { get; init; }
    public required string Id { get; init; }
    public required string Date { get; init; }
    public required AnnouncementBtnInfoModel? Btn1 { get; init; }
    public required AnnouncementBtnInfoModel? Btn2 { get; init; }
}

public record AnnouncementBtnInfoModel
{
    public required string Text { get; init; }
    public required string Command { get; init; }
    public required string CommandParameter { get; init; }
}