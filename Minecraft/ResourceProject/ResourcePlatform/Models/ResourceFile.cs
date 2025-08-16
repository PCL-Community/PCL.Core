namespace PCL.Core.Minecraft.ResourceProject.ResourcePlatform.Models;

public class ResourceFile
{
    private string? _DownloadUrl;

    /// <summary>
    /// 资源的直接下载地址
    /// </summary>
    public string? DownloadUrl {
        get
        {
            if (string.IsNullOrEmpty(_DownloadUrl) && FileSource is ProjectSource.CurseForge)
            {
                _DownloadUrl = "https://edge.forgecen.net";
                return _DownloadUrl;
            }

            return _DownloadUrl;
        }
        set => _DownloadUrl = value;
    }

    public ProjectSource FileSource { get; set; }

    public required string Algorithm;

    public required int Size;

    public required string FileHash;

}