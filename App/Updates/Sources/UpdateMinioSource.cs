using PCL.Core.App.Updates.Models;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Net.Downloader;
using PCL.Core.Net.Http.Client;

namespace PCL.Core.App.Updates.Sources;

public class UpdateMinioSource(string baseUrl, string name = "Minio") : IUpdateSource
{
    public bool IsAvailable => !string.IsNullOrEmpty(baseUrl);

    public string SourceName => name;

    private Dictionary<string, string>? _remoteCache;

    private static readonly string _TempPath = Path.Combine(FileService.TempPath, "Cache", "Update");

    /// <exception cref="ArgumentException">Throws if version info is null.</exception>
    /// <inheritdoc/>
    public async Task<VersionDataModel> CheckUpdateAsync()
    {
        // 获取版本信息
        LogWrapper.Info("Update", "开始获取版本信息");
        var versionJsonData = await _GetVersionJsonDataAsync().ConfigureAwait(false);

        if (versionJsonData is null)
        {
            throw new ArgumentException("Version info cannot be null", nameof(versionJsonData));
        }

        return new VersionDataModel
        {
            VersionCode = versionJsonData["version"]!["code"]!.GetValue<int>(),
            VersionName = versionJsonData["version"]!["name"]!.GetValue<string>(),
            Sha256 = versionJsonData["sha256"]!.GetValue<string>(),
            ChangeLog = versionJsonData["changelog"]!.GetValue<string>(),
            Source = SourceName
        };
    }


    #region Download Workflow

    /// <inheritdoc/>
    public async Task DownloadAsync(string outputPath)
    {
        _LogInfo("Start try to download update");

        var versionInfo = await _GetVersionInfoAsync().ConfigureAwait(false);
        var tempDownloadDir = _PrepareTempDirectory();

        var downloadItem = await _CreateDownloadItemAsync(versionInfo, tempDownloadDir).ConfigureAwait(false);

        _LogInfo("Start to download");
        
        var manager = new DownloadManager(new FastMirrorSelector(new HttpClient()));
        await manager.DownloadAsync(downloadItem, CancellationToken.None).ConfigureAwait(false);

        _LogInfo("Successfully download update file");
    }

    private async Task<JsonObject> _GetVersionInfoAsync()
    {
        _LogInfo("Start to get version info");

        var versionJson = await _GetVersionJsonDataAsync().ConfigureAwait(false);
        if (versionJson is null)
        {
            throw new InvalidDataException("Version info cannot be null");
        }

        if (string.IsNullOrEmpty(versionJson["sha256"]?.GetValue<string>()))
        {
            throw new InvalidDataException("Remote version info not contains 'sha256'");
        }

        _LogInfo("Successfully get version info");

        return versionJson;
    }

    private async Task<DownloadTask> _CreateDownloadItemAsync(JsonObject versionJson, string tempDir)
    {
        var updateSha256 = versionJson["sha256"]!.GetValue<string>();
        var selfSha256 = await Files.GetFileSHA256Async(Basics.ExecutablePath).ConfigureAwait(false);

        var patchFileName = $"{selfSha256}-{updateSha256}-patch";
        var patches = versionJson["patches"]?.AsArray();

        if (patches is not null && patches.Contains(patchFileName))
        {
            _LogInfo("Get accessible patch update");
            var tempPath = Path.Combine(tempDir, patchFileName);

            return new DownloadTask(
                new Uri($"{baseUrl}static/patch/{patchFileName}"),
                tempPath
            );
        }

        _LogInfo("Not found accessible patch update. Use fill-pachage instead");

        var downloads = versionJson["downloads"]?.AsArray();
        if (downloads is null || downloads.Count == 0)
        {
            throw new InvalidDataException("Not found remote version info download Uri");
        }

        var downloadUrl = RandomUtils.PickRandom(
            downloads.Select(d => d!.GetValue<string>()).ToList()
        );

        var fullPackagePath = Path.Combine(tempDir, $"{updateSha256}.bin");
        return new DownloadTask(new Uri(downloadUrl), fullPackagePath);
    }

    #endregion

    #region Announcement

    /// <inheritdoc/>
    public async Task<VersionAnnouncementDataModel> GetAnnouncementAsync()
    {
        _LogInfo("开始获取公告列表");
        var jsonData = (await _GetRemoteInfoByNameAsync("announcement").ConfigureAwait(false))!
            ["content"]!.AsArray();
        _LogInfo("公告列表获取完成");

        return new VersionAnnouncementDataModel(jsonData.Select(_ConvertAnnouncementDto).ToList());
    }

    private static VersionAnnouncementContentModel _ConvertAnnouncementDto(JsonNode? node)
    {
        if (node is not JsonObject obj)
            throw new InvalidDataException("公告 JSON 节点不是对象");

        return new VersionAnnouncementContentModel
        {
            Title = obj["title"]!.GetValue<string>(),
            Detail = obj["detail"]!.GetValue<string>(),
            Id = obj["id"]!.GetValue<string>(),
            Date = obj["date"]!.GetValue<string>(),

            Btn1 = _ConvertButton(obj["btn1"]),
            Btn2 = _ConvertButton(obj["btn2"])
        };
    }

    private static AnnouncementBtnInfoModel? _ConvertButton(JsonNode? btnNode)
    {
        if (btnNode is not JsonObject btn)
            return null;

        return new AnnouncementBtnInfoModel
        {
            Text = btn["text"]!.GetValue<string>(),
            Command = btn["command"]!.GetValue<string>(),
            CommandParameter = btn["command_paramter"]!.GetValue<string>()
        };
    }

    #endregion


    #region Remote Version

    /// <summary>
    /// 通过名称获取远程信息
    /// </summary>
    /// <param name="versionName">名称</param>
    /// <param name="path">保存路径</param>
    /// <returns>远程信息</returns>
    private async Task<JsonObject?> _GetRemoteInfoByNameAsync(string versionName, string path = "")
    {
        // 尝试从远程 cache.json 获取预期哈希并命中本地缓存
        Dictionary<string, string>? remoteCache = null;
        try
        {
            remoteCache = await _GetRemoteCacheAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _LogWarning("拉取远程 cache 失败，继续尝试远程获取具体信息", ex);
        }

        var localCachepath = Path.Combine(_TempPath, $"{versionName}.json");

        // match local cache
        if (remoteCache != null && remoteCache.TryGetValue(versionName, out var expectedHash))
        {
            if (await _ValidateCacheAsync(versionName, expectedHash).ConfigureAwait(false))
            {
                var cachedJson = await _TryGetLocalCacheAsync(localCachepath).ConfigureAwait(false);
                if (cachedJson is not null)
                {
                    return cachedJson;
                }
            }
        }

        // get remote information
        var (remoteContent, remoteJson) = await _FetchRemoteInfoAsync(path, versionName).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(remoteContent))
        {
            await _SaveLocalCacheAsync(localCachepath, remoteContent).ConfigureAwait(false);
        }
        else
        {
            _LogInfo("Remote content is null. Skip save content to local cache file");
        }

        return remoteJson;
    }

    private async Task<Dictionary<string, string>> _GetRemoteCacheAsync()
    {
        if (_remoteCache != null) return _remoteCache;

        _LogTrace("正在拉取远程缓存...");

        var builder = HttpRequestBuilder.Create($"{baseUrl}apiv2/cache.json", HttpMethod.Get);
        var response = (await builder.SendAsync().ConfigureAwait(false)).AsStringContent();

        _LogTrace("远程缓存拉取完成");

        var parsed = JsonNode.Parse(response);
        if (parsed is not JsonObject remoteCache)
        {
            _LogError("无法解析远程缓存 JSON");
            throw new JsonException("远程缓存解析失败");
        }

        _LogTrace("正在赋值远程缓存...");

        var dict = remoteCache.ToDictionary(
            pair => pair.Key,
            pair => pair.Value?.GetValue<string>() ?? string.Empty
        );

        _LogTrace("远程缓存赋值完成");
        _remoteCache = dict;
        return dict;
    }

    private async Task<JsonObject?> _TryGetLocalCacheAsync(string cacheFilePath)
    {
        try
        {
            _LogTrace("正在读取本地缓存信息...");
            var localContent = await File.ReadAllTextAsync(cacheFilePath).ConfigureAwait(false);
            _LogTrace("本地缓存信息读取完成");

            var localJson = JsonNode.Parse(localContent);
            if (localJson is JsonObject cacheObject)
            {
                _LogTrace("本地缓存信息解析成功，使用本地缓存");
                return cacheObject;
            }

            _LogWarning("本地缓存解析失败，从远程获取信息");
        }
        catch (Exception ex)
        {
            _LogWarning("本地缓存读取或校验失败，从远程获取信息", ex);
        }

        return null;
    }

    private async Task<(string? Content, JsonObject? InfoJson)> _FetchRemoteInfoAsync(string path, string versionName)
    {
        _LogTrace("正在从远程获取信息...");
        var builder = HttpRequestBuilder.Create($"{baseUrl}apiv2/{path}{versionName}.json", HttpMethod.Get);
        var remoteContent = (await builder.SendAsync().ConfigureAwait(false)).AsStringContent();
        _LogTrace("远程信息获取完成");

        var parsed = JsonNode.Parse(remoteContent);
        if (parsed is not JsonObject remoteJson)
        {
            _LogWarning("无法解析远程信息 JSON");
            return (null, null);
        }

        return (remoteContent, remoteJson);
    }

    private async Task _SaveLocalCacheAsync(string filePath, string content)
    {
        try
        {
            _LogTrace("正在缓存远程信息到本地...");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? FileService.TempPath);
            await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);
            _LogTrace("远程信息缓存完成");
        }
        catch (Exception ex)
        {
            _LogWarning("本地缓存写入失败，仍返回远端信息", ex);
        }
    }

    private async Task<bool> _ValidateCacheAsync(string versionName, string expectedHash)
    {
        if (string.IsNullOrEmpty(versionName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }

        return await _IsValidCacheAsync($"{versionName}.json", expectedHash).ConfigureAwait(false);
    }

    /// <summary>
    /// 缓存是否有效
    /// </summary>
    /// <param name="fileName">缓存文件名</param>
    /// <param name="expectedHash">预期哈希值</param>
    /// <returns>是否有效</returns>
    private static async Task<bool> _IsValidCacheAsync(string fileName, string expectedHash)
    {
        var cacheFile = Path.Combine(_TempPath, fileName);
        var fileInfo = new FileInfo(cacheFile);

        return fileInfo.Exists &&
               (DateTime.Now - fileInfo.LastWriteTime).TotalHours <= 1 &&
               await Files.GetFileMD5Async(cacheFile).ConfigureAwait(false) == expectedHash;
    }

    #endregion

    /// <summary>
    /// 获取该通道的版本信息
    /// </summary>
    /// <exception cref="HttpRequestException">Throws if failed to get version info from remote server</exception>
    /// <returns>版本信息</returns>
    private async Task<JsonObject?> _GetVersionJsonDataAsync()
    {
        try
        {
            _LogTrace("开始获取版本 Json 信息");
            var channelName = _GetChannelName();
            var assets = (await _GetRemoteInfoByNameAsync($"updates-{channelName}", "updates/")
                .ConfigureAwait(false))?["assets"]?.AsArray();
            _LogTrace("版本 Json 信息获取完成");

            return assets?.FirstOrDefault()?.AsObject();
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("获取版本信息失败", ex);
        }
    }

    /// <summary>
    /// 获取通道名称
    /// </summary>
    /// <returns>通道名称</returns>
    private static string _GetChannelName()
    {
        var channelName = string.Empty;
        channelName += Config.System.Update.UpdateChannel switch
        {
            0 => "sr",
            1 => "fr",
            _ => "sr"
        };

        channelName += RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

        return channelName;
    }


    private static string _PrepareTempDirectory()
    {
        var path = Path.Combine(_TempPath, "Download");
        Directory.CreateDirectory(path);

        return path;
    }

    #region Logger Wrapper

    private void _LogInfo(string msg)
    {
        LogWrapper.Info("Update", msg);
    }

    private void _LogWarning(string msg, Exception? ex = null)
    {
        LogWrapper.Warn(ex, "Update", msg);
    }

    private void _LogError(string msg, Exception? ex = null)
    {
        LogWrapper.Error(ex, "Update", msg);
    }

    private void _LogTrace(string msg)
    {
        LogWrapper.Trace("Update", msg);
    }

    #endregion
}