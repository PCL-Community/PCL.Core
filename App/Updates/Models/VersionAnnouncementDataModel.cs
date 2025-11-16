using System.Collections.Generic;

namespace PCL.Core.App.Updates.Models;

public class VersionAnnouncementDataModel
{
    public required List<VersionAnnouncementContentModel> Contents { get; set; }
}

public class VersionAnnouncementContentModel
{
    public required string Title { get; set; }
    public required string Detail { get; set; }
    public required string Id { get; set; }
    public required string Date { get; set; }
    public required AnnouncementBtnInfoModel Btn1 { get; set; }
    public required AnnouncementBtnInfoModel Btn2 { get; set; }
}

public class AnnouncementBtnInfoModel
{
    public required string Text { get; set; }
    public required string Command { get; set; }
    public required string CommandParameter { get; set; }
}