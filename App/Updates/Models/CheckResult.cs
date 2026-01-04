namespace PCL.Core.App.Updates.Models;

public record CheckResult(CheckResultType Type, VersionDataModel? VersionData = null);

public enum CheckResultType
{
    /// <summary>有新版本</summary>
    Available,

    /// <summary>无新版本</summary>
    Latest
}