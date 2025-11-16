namespace PCL.Core.App.Updates;

public class VersionDataModel
{
    public required string VersionName { get; set; }
    public required int VersionCode { get; set; }
    public required string SHA256 { get; set; }
    public required string ChangeLog { get; set; }
    public required string Source { get; set; }
}