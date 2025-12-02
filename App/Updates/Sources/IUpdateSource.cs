using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Models;

namespace PCL.Core.App.Updates.Sources;

public interface IUpdateSource
{
    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns>检查更新结果</returns>
    public Task<CheckUpdateResult> CheckUpdateAsync();

    /// <summary>
    /// 获取版本公告列表
    /// </summary>
    /// <returns>版本公告列表</returns>
    public Task<VersionAnnouncementDataModel?> GetAnnouncementListAsync();

    /// <summary>
    /// 下载更新文件
    /// </summary>
    /// <param name="outputPath">输出路径</param>
    public Task<bool> DownloadAsync(string outputPath);
    
    /// <summary>
    /// 更新源名称
    /// </summary>
    public string SourceName { get; set; }
    
    /// <summary>
    /// 更新源是否可用
    /// </summary>
    public bool IsAvailable { get; }
}

public enum CheckUpdateResultType
{
    /// <summary>
    /// 有新版本
    /// </summary>
    HasNewVersion,
    
    /// <summary>
    /// 无新版本
    /// </summary>
    NoNewVersion,
    
    /// <summary>
    /// 检查更新失败
    /// </summary>
    CheckFailed
}

public record CheckUpdateResult(CheckUpdateResultType Type, VersionDataModel? VersionData = null);