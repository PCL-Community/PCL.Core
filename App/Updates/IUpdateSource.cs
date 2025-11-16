using System;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Models;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates;

public interface IUpdateSource
{
    /// <summary>
    /// 是否可用, 根据本地情况判断
    /// </summary>
    /// <returns>是否可用</returns>
    public bool IsAvailable();

    /// <summary>
    /// 确保是最新版本
    /// </summary>
    /// <returns>True 表示更新成功, False 表示没有数据更新</returns>
    public bool RefreshCache();

    /// <summary>
    /// 获取最新版本信息
    /// </summary>
    /// <param name="channel">更新通道</param>
    /// <param name="arch">更新架构</param>
    /// <returns>最新版本信息</returns>
    public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch);

    /// <summary>
    /// 判断是否为最新版本
    /// </summary>
    /// <param name="channel">更新通道</param>
    /// <param name="arch">更新架构</param>
    /// <param name="currentVersion">当前版本号</param>
    /// <param name="currentVersionCode">当前版本代码</param>
    /// <returns>是否为最新版本</returns>
    public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode);

    /// <summary>
    /// 获取版本公告列表
    /// </summary>
    /// <returns>版本公告列表</returns>
    public VersionAnnouncementDataModel GetAnnouncementList();

    /// <summary>
    /// 下载更新文件
    /// </summary>
    /// <param name="channel">更新通道</param>
    /// <param name="arch">更新架构</param>
    /// <param name="outputPath">输出路径</param>
    public Task DownloadAsync(UpdateChannel channel, UpdateArch arch, string outputPath);
    
    public string SourceName { get; set; }
}