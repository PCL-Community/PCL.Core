using PCL.Core.App.Updates.Models;

namespace PCL.Core.App.Updates.Sources;

public record CheckResult(CheckResultType Type, VersionDataModel? VersionData = null);

public enum CheckResultType
{
    /// <summary>有新版本</summary>
    Available,

    /// <summary>无新版本</summary>
    Latest
}