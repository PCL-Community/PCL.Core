using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Sources;

public interface IUpdateSource
{
    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns>检查更新结果</returns>
    public Task<VersionData> CheckUpdateAsync();

    /// <summary>
    /// 获取版本公告列表
    /// </summary>
    /// <returns>版本公告列表</returns>
    public Task<VersionAnnouncementDataModel> GetAnnouncementAsync();

    /// <summary>
    /// 下载更新文件
    /// </summary>
    /// <param name="outputPath">输出路径</param>
    public Task DownloadAsync(string outputPath);
    
    /// <summary>
    /// 更新源名称
    /// </summary>
    public string SourceName { get; }
    
    /// <summary>
    /// 更新源是否可用
    /// </summary>
    public bool IsAvailable { get; }
}