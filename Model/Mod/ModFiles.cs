namespace PCL.Core.Model.Mod;

public record ModFiles(
    string Hash,
    string DownloadUrl,
    string FileName,
    long FileSize);