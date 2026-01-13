using PCL.Core.App.Updates.Models;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Net.Downloader;
using PCL.Core.Net.Http.Client;
using PCL.Core.Utils.Diff;

namespace PCL.Core.App.Updates.Sources;

public class UpdateMinioSource(string baseUrl, string name = "Minio") : IUpdateSource
{
    #region Data Models

    private sealed record VersionAssetsDataModel
    {
        [JsonPropertyName("assets")] public required VersionDataModel[] Assets { get; init; }
    }

    private sealed record VersionDataModel
    {
        [JsonPropertyName("version")] public required VersionInfoDataModel Version { get; init; }
    
        [JsonPropertyName("sha256")] public required string Sha256 { get; init; }
    
        [JsonPropertyName("changelog")] public required string ChangeLog { get; init; }
    
        [JsonPropertyName("patches")] public required string[] Patches { get; init; }
    
        [JsonPropertyName("downloads")] public required string[] Downloads { get; init; }
    }

    private sealed record VersionInfoDataModel
    {
        [JsonPropertyName("name")] public required string Name { get; init; }
    
        [JsonPropertyName("code")] public required int Code { get; init; }
    }
    
    #endregion
    
    public bool IsAvailable => !string.IsNullOrWhiteSpace(baseUrl);

    public string SourceName => name;

    private static readonly string _TempPath = Path.Combine(FileService.TempPath, "Cache", "Update");
    
    private VersionDataModel? _cachedVersionInfo;
    
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Throws if version info is null.</exception>
    /// <exception cref="HttpRequestException">Throws if failed to get version info from remote server</exception>
    public async Task<VersionData> CheckUpdateAsync()
    {
        try
        {
            _LogTrace("Start to get version Json info");
            var channelName = _GetChannelName();
            var assets = await _GetRemoteInfoByNameAsync<VersionAssetsDataModel>($"updates-{channelName}", "updates/")
                .ConfigureAwait(false);
            _LogTrace("Version Json info get completed");

            _cachedVersionInfo = assets?.Assets.FirstOrDefault();
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Failed to get version info from remote server", ex);
        }

        return _cachedVersionInfo is null
            ? throw new InvalidDataException("Not found remote version info")
            : new VersionData
            {
                VersionName = _cachedVersionInfo.Version.Name,
                VersionCode = _cachedVersionInfo.Version.Code,
                Sha256 = _cachedVersionInfo.Sha256,
                ChangeLog = _cachedVersionInfo.ChangeLog
            };
    }

    #region Download Workflow

    public async Task DownloadAsync(string outputPath)
    {
        _LogInfo("Start try to download update");

        var tempDownloadDir = _PrepareTempDirectory();
        var (task, isPatch) = await _CreateDownloadTaskAsync(
            _cachedVersionInfo ?? throw new InvalidOperationException("Version info is null, haven't checked update"), 
            tempDownloadDir).ConfigureAwait(false);

        _LogInfo("Start to download");
        var manager = new DownloadManager(new FastMirrorSelector(new HttpClient()));
        await manager.DownloadAsync(task, CancellationToken.None).ConfigureAwait(false);

        _LogInfo("Successfully download update file and start to use update file");
        await _UseUpdateFileAsync(task.TargetPath, outputPath, isPatch).ConfigureAwait(false);
        _LogInfo("Successfully use update file");
    }

    private async Task<(DownloadTask task, bool isPatch)> _CreateDownloadTaskAsync(
        VersionDataModel versionJson,
        string tempDir)
    {
        var updateSha256 = versionJson.Sha256;
        var selfSha256 = await Files.GetFileSHA256Async(Basics.ExecutablePath).ConfigureAwait(false);

        var patchFileName = $"{selfSha256}_{updateSha256}.patch";
        var patches = versionJson.Patches;

        if (patches.Contains(patchFileName))
        {
            _LogInfo("Get accessible patch update");
            var tempPath = Path.Combine(tempDir, patchFileName);

            return (new DownloadTask(
                new Uri($"{baseUrl}static/patch/{patchFileName}"),
                tempPath
            ), true);
        }

        _LogInfo("Not found accessible patch update. Use fill-package instead");

        var downloads = versionJson.Downloads;
        if (downloads is null || downloads.Length == 0)
        {
            throw new InvalidDataException("Not found remote version info download Uri");
        }

        return (new DownloadTask(
                new Uri(RandomUtils.PickRandom(downloads)),
                Path.Combine(tempDir, $"{updateSha256}.bin")),
            false);
    }

    private static async Task _UseUpdateFileAsync(string updateFilePath, string outputPath, bool isPatch)
    {
        if (isPatch)
        {
            var diff = new BsDiff();
            var baseBytes = await Files.ReadAllBytesOrEmptyAsync(Basics.ExecutablePath).ConfigureAwait(false);
            var patchBytes = await Files.ReadAllBytesOrEmptyAsync(updateFilePath).ConfigureAwait(false);
            var newFile = await diff.ApplyAsync(baseBytes, patchBytes).ConfigureAwait(false);
            await Files.WriteFileAsync(outputPath, newFile).ConfigureAwait(false);
            return;
        }

        await using var fs = new FileStream(updateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

        var entry = _FindExecutableEntry(zip);
        if (entry is null)
            throw new InvalidDataException("Executable entry not found in update package");

        entry.ExtractToFile(outputPath, overwrite: true); 
    }


    private static ZipArchiveEntry? _FindExecutableEntry(ZipArchive zip) =>
        zip.Entries.FirstOrDefault(e =>
            e.Name.Contains("Plain Craft Launcher Community Edition.exe", StringComparison.OrdinalIgnoreCase)) ??
        zip.Entries.FirstOrDefault(e =>
            e.Name.Contains("Plain Craft Launcher", StringComparison.OrdinalIgnoreCase)) ??
        zip.Entries.FirstOrDefault(e =>
            e.Name.Contains("Launcher", StringComparison.OrdinalIgnoreCase)) ??
        zip.Entries.FirstOrDefault(e =>
            e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

    private static string _PrepareTempDirectory()
    {
        var path = Path.Combine(_TempPath, "Download");
        Directory.CreateDirectory(path);

        return path;
    }

    #endregion

    #region Announcement

    /// <inheritdoc/>
    public async Task<VersionAnnouncementDataModel> GetAnnouncementAsync()
    {
        VersionAnnouncementDataModel? ret;
        try
        {
            _LogTrace("Start to get announcement Json info");
            ret = await _GetRemoteInfoByNameAsync<VersionAnnouncementDataModel>("announcement")
                .ConfigureAwait(false);
            _LogTrace("Announcement Json info get completed");
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Failed to get announcement info from remote server", ex);
        }

        return ret ?? throw new InvalidDataException("Not found remote announcement info");
    }

    #endregion

    /// <summary>
    /// 通过名称获取远程信息
    /// </summary>
    /// <param name="path">保存路径</param>
    /// <param name="versionName">名称</param>
    /// <returns>远程信息</returns>
    private async Task<T?> _GetRemoteInfoByNameAsync<T>(string versionName, string path = "")
    {
        _LogTrace("Fetching remote info...");

        var builder = HttpRequestBuilder.Create($"{baseUrl}apiv2/{path}{versionName}.json", HttpMethod.Get);
        var result = await builder.SendAsync().ConfigureAwait(false);
        var remoteJson = await result.AsJsonAsync<T>().ConfigureAwait(false);

        _LogTrace("Remote info fetched");

        return remoteJson;
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
