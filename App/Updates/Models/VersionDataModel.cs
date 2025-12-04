namespace PCL.Core.App.Updates.Models;

public class VersionDataModel
{
    public required string VersionName { get; init; }
    public required int VersionCode { get; init; }
    public required string Sha256 { get; init; }
    public required string ChangeLog { get; init; }
    public required string Source { get; init; }
}