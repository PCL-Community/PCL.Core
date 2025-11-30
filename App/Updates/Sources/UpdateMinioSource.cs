using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App.Updates.Models;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;

namespace PCL.Core.App.Updates.Sources;

public class UpdateMinioSource(string baseUrl, string name = "Minio") : IUpdateSource
{
    public bool IsAvailable => !string.IsNullOrEmpty(baseUrl);
    
    public string SourceName { get; set; } = name;

    private async Task<Dictionary<string, string>> _GetRemoteCacheAsync()
    {
        try
        {
            LogWrapper.Info("Update", "正在拉取远程缓存...");
            var builder = HttpRequestBuilder.Create($"{baseUrl}apiv2/cache.json", HttpMethod.Get);
            var response = (await builder.SendAsync().ConfigureAwait(false)).AsStringContent();
            LogWrapper.Info("Update", "远程缓存拉取完成");

            var parsed = JsonNode.Parse(response);
            if (parsed is not JsonObject remoteCache)
            {
                LogWrapper.Error("Update", "无法解析远程缓存 JSON");
                throw new InvalidOperationException("远程缓存解析失败");
            }

            LogWrapper.Info("Update", "正在赋值远程缓存...");
            var dict = remoteCache.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.GetValue<string>() ?? string.Empty
            );
            LogWrapper.Info("Update", "远程缓存赋值完成");
            return dict;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("获取远程缓存失败", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CheckUpdateResult> CheckUpdateAsync()
    {
        try
        {
            // 获取版本信息
            LogWrapper.Info("Update", "开始获取版本信息");
            var versionJsonData = await _GetVersionJsonData().ConfigureAwait(false);
            var lastestVersion = new VersionDataModel
            {
                VersionCode = versionJsonData["version"]!["code"]!.GetValue<int>(),
                VersionName = versionJsonData["version"]!["name"]!.GetValue<string>(),
                Sha256 = versionJsonData["sha256"]!.GetValue<string>(),
                ChangeLog = versionJsonData["changelog"]!.GetValue<string>(),
                Source = SourceName
            };
            LogWrapper.Info("Update", 
                $"获取到最新版本信息：{lastestVersion.VersionName} " + 
                $"({lastestVersion.VersionCode})");
            LogWrapper.Info("Update",
                $"当前版本：{Basics.VersionName} " + 
                $"({Basics.VersionNumber})");

            return Basics.VersionNumber != lastestVersion.VersionCode   // 比较版本号
                ? new CheckUpdateResult(CheckUpdateResultType.HasNewVersion, lastestVersion)
                : new CheckUpdateResult(CheckUpdateResultType.NoNewVersion);
        }
        catch (NullReferenceException nre)
        {
            LogWrapper.Warn(nre, "Update", $"检查更新失败，可能是远程数据格式有误");
            return new CheckUpdateResult(CheckUpdateResultType.CheckFailed);
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", $"检查更新失败");
            return new CheckUpdateResult(CheckUpdateResultType.CheckFailed);
        }
    }

    /// <inheritdoc/>
    public async Task DownloadAsync(string outputPath)
    {
        try
        {
            bool patchUpdate;
            var tempPathBase = Path.Combine(Basics.TempPath, "Cache", "Update", "Download");
            Directory.CreateDirectory(tempPathBase);

            LogWrapper.Info("Update", "开始获取版本信息");
            var versionJsonData = await _GetVersionJsonData().ConfigureAwait(false);
            if (versionJsonData is null) throw new InvalidOperationException("版本信息为空");

            var selfSha256 = await Files.GetFileSHA256Async(Basics.ExecutableName).ConfigureAwait(false);
            var updateSha256 = versionJsonData["sha256"]?.GetValue<string>();
            if (string.IsNullOrEmpty(updateSha256)) throw new InvalidOperationException("远程版本信息缺少 sha256");

            var patchFileName = $"{selfSha256}-{updateSha256}.patch";
            var patches = versionJsonData["patches"]?.AsArray();
            LogWrapper.Info("Update", "版本信息获取完成");

            LogWrapper.Info("Update", "开始下载更新文件");
            var downloader = new Downloader();
            DownloadItem downloadItem;

            if (patches != null && patches.Contains(patchFileName))
            {
                patchUpdate = true;
                var tempPath = Path.Combine(tempPathBase, patchFileName);
                downloadItem = new DownloadItem(new Uri($"{baseUrl}static/patch/{patchFileName}"), tempPath);
            }
            else
            {
                patchUpdate = false;
                var tempPath = Path.Combine(tempPathBase, $"{updateSha256}.bin");
                var downloads = versionJsonData["downloads"]?.AsArray();
                if (downloads == null || downloads.Count == 0) throw new InvalidOperationException("远程版本信息缺少下载地址");
                var downloadUrl = RandomUtils.PickRandom(
                    downloads.Select(item => item!.GetValue<string>()).ToList()
                );
                downloadItem = new DownloadItem(new Uri(downloadUrl), tempPath);
            }

            downloader.AddItem(downloadItem);
            downloader.Start();
            LogWrapper.Info("Update", "下载器已启动");

            // 用异步轮询替代忙等，降低 CPU 占用
            while (true)
            {
                var status = downloadItem.Status;
                if (status is DownloadItemStatus.Success) break;
                if (status is DownloadItemStatus.Failed or DownloadItemStatus.Cancelled)
                {
                    LogWrapper.Warn("Update", "更新文件下载失败或被取消");
                    return;
                }
                await Task.Delay(200).ConfigureAwait(false);
            }

            LogWrapper.Info("Update", "更新文件下载完成");
        }
        catch (InvalidOperationException ioe)
        {
            LogWrapper.Warn(ioe, "Update", "下载更新失败（数据不完整或格式问题）");
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", "下载更新失败");
        }
    }

    /// <inheritdoc/>
    public async Task<VersionAnnouncementDataModel?> GetAnnouncementListAsync()
    {
        try
        {
            LogWrapper.Info("Update", "开始获取公告列表");
            var jsonData = (await _GetRemoteInfoByName("announcement").ConfigureAwait(false))!
                ["content"]!.AsArray();
            LogWrapper.Info("Update", "公告列表获取完成");
            
            return new VersionAnnouncementDataModel
            {
                Contents = jsonData.Select(item =>
                {
                    var btn1Json = item!["btn1"]?.AsObject();
                    var btn2Json = item["btn2"]?.AsObject();

                    return new VersionAnnouncementContentModel
                    {
                        Title = item["title"]!.GetValue<string>(),
                        Detail = item["detail"]!.GetValue<string>(),
                        Id = item["id"]!.GetValue<string>(),
                        Date = item["date"]!.GetValue<string>(),
                        Btn1 = btn1Json != null
                            ? new AnnouncementBtnInfoModel
                            {
                                Text = btn1Json["text"]!.GetValue<string>(),
                                Command = btn1Json["command"]!.GetValue<string>(),
                                CommandParameter = btn1Json["command_paramter"]!.GetValue<string>()
                            }
                            : null,
                        Btn2 = btn2Json != null 
                            ? new AnnouncementBtnInfoModel
                            {
                                Text = btn2Json["text"]!.GetValue<string>(),
                                Command = btn2Json["command"]!.GetValue<string>(),
                                CommandParameter = btn2Json["command_paramter"]!.GetValue<string>()
                            }
                            : null
                    };
                }).ToList()
            };
        }
        catch (NullReferenceException nre)
        {
            LogWrapper.Warn(nre, "Update", "获取公告列表失败，可能是远程数据格式有误");
            return null;
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", "获取公告列表失败");
            return null;
        }
    }
    
    /// <summary>
    /// 获取该通道的版本信息
    /// </summary>
    /// <returns>版本信息</returns>
    private async Task<JsonObject> _GetVersionJsonData()
    {
        try
        {
            LogWrapper.Info("Update", "开始获取版本 Json 信息");
            var channelName = _GetChannelName();
            var assets = (await _GetRemoteInfoByName($"updates-{channelName}", "updates/")
                .ConfigureAwait(false))!["assets"]!.AsArray();
            LogWrapper.Info("Update", "版本 Json 信息获取完成");

            return assets.FirstOrDefault()!.AsObject();
        }
        catch (NullReferenceException nre)
        {
            throw new InvalidOperationException("获取版本信息失败，可能是远程数据格式有误", nre);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("获取版本信息失败", ex);
        }
    }
    
    /// <summary>
    /// 获取通道名称
    /// </summary>
    /// <returns>通道名称</returns>
    private static string _GetChannelName()
    {
        var channelName = string.Empty;
        channelName += Basics.CurrentUpdateChannel switch
        {
            UpdateChannel.Stable => "sr",
            UpdateChannel.Beta => "fr",
            _ => "sr"
        };

        channelName += Basics.IsArm64 ? "arm64" : "x64";

        return channelName;
    }
    
    /// <summary>
    /// 通过名称获取远程信息
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="path">保存路径</param>
    /// <returns>远程信息</returns>
   private async Task<JsonObject?> _GetRemoteInfoByName(string name, string path = "") 
    {
        var localInfoFile = Path.Combine(Basics.TempPath, "Cache", "Update", $"{name}.json");

        // 尝试从远程 cache.json 获取预期哈希并命中本地缓存
        Dictionary<string, string>? remoteCache = null;
        try
        {
            remoteCache = await _GetRemoteCacheAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", "拉取远程 cache 失败，继续尝试远程获取具体信息");
        }

        if (remoteCache != null && remoteCache.TryGetValue(name, out var expectedHash))
        {
            try
            {
                if (await _IsCacheValidAsync($"{name}.json", expectedHash).ConfigureAwait(false))
                {
                    LogWrapper.Info("Update", "正在读取本地缓存信息...");
                    var localContent = await File.ReadAllTextAsync(localInfoFile).ConfigureAwait(false);
                    LogWrapper.Info("Update", "本地缓存信息读取完成");

                    var parsedLocal = JsonNode.Parse(localContent);
                    if (parsedLocal is JsonObject json)
                    {
                        LogWrapper.Info("Update", "本地缓存信息解析成功，使用本地缓存");
                        return json;
                    }

                    LogWrapper.Warn("Update", "本地缓存解析失败，从远程获取信息");
                }
            }
            catch (Exception ex)
            {
                LogWrapper.Warn(ex, "Update", "本地缓存读取或校验失败，从远程获取信息");
            }
        }

        // 发送 GET 请求获取远程信息
        LogWrapper.Info("Update", "正在从远程获取信息...");
        var builder = HttpRequestBuilder.Create($"{baseUrl}apiv2/{path}{name}.json", HttpMethod.Get);
        var response = (await builder.SendAsync().ConfigureAwait(false)).AsStringContent();
        LogWrapper.Info("Update", "远程信息获取完成");

        var parsed = JsonNode.Parse(response);
        if (parsed is not JsonObject remoteInfo)
        {
            LogWrapper.Warn("Update", "无法解析远程信息 JSON");
            return null;
        }

        // 缓存到本地
        try
        {
            LogWrapper.Info("Update", "正在缓存远程信息到本地...");
            Directory.CreateDirectory(Path.GetDirectoryName(localInfoFile) ?? Basics.TempPath);
            await File.WriteAllTextAsync(localInfoFile, response).ConfigureAwait(false);
            LogWrapper.Info("Update", "远程信息缓存完成");
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Update", "本地缓存写入失败（非致命），仍返回远端信息");
        }

        return remoteInfo;
    }
    
    /// <summary>
    /// 缓存是否有效
    /// </summary>
    /// <param name="fileName">缓存文件名</param>
    /// <param name="expectedHash">预期哈希值</param>
    /// <returns>是否有效</returns>
    private static async Task<bool> _IsCacheValidAsync(string fileName, string expectedHash)
    {
        var cacheFile = Path.Combine(Basics.TempPath, "Cache", "Update", fileName);
        var fileInfo = new FileInfo(cacheFile);
        return fileInfo.Exists &&
               (DateTime.Now - fileInfo.LastWriteTime).TotalHours >= 1 &&
               await Files.GetFileMD5Async(cacheFile).ConfigureAwait(false) == expectedHash;
    }

}