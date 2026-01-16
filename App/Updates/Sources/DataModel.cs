using System.Text.Json.Serialization;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Sources;

public sealed record VersionInfoData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] int Code);

public sealed record VersionData (
    [property: JsonPropertyName("version")] VersionInfoData Version,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("changelog")] string ChangeLog,
    [property: JsonPropertyName("patches")]  string[] Patches,
    [property: JsonPropertyName("downloads")] string[] Downloads
) {
    public bool IsAvailable => Version.Code > Basics.VersionCode &&
                               SemVer.Parse(Version.Name) > SemVer.Parse(Basics.VersionName);
}

public record AnnouncementsList(
    [property: JsonPropertyName("content")] AnnouncementContent[] Contents
);

public record AnnouncementContent(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("btn1")] AnnouncementBtnInfo? Btn1,
    [property: JsonPropertyName("btn2")] AnnouncementBtnInfo? Btn2
);

public record AnnouncementBtnInfo (
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("command_paramter")] string CommandParameter
);