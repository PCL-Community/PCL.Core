using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.ProgramSetup;
using PCL.Core.UI;
using PCL.Core.Utils;
using Architecture = System.Runtime.InteropServices.Architecture;

namespace PCL.Core.App.Update;

public class UpdateManager
{
    public static readonly UpdateManager Instance = new();

    private static readonly string[] _UpdateBranchString = ["sr", "fr"];

    private static readonly MirrorChyanSource _MirrorChyanSource = new(Setup.System.MirrorChyanKey);

    private static readonly IUpdateSource[] _SourceInstance =
    [
        new CommunitySource("Pysio", "https://s3.pysio.online/pcl2-ce/"),
        new CommunitySource("Naids", "https://staticassets.naids.com/resources/pclce/"),
        new CommunitySource("GitHub", "https://github.com/PCL-Community/PCL2_CE_Server/raw/main/")
    ];

    private static readonly SemVer _LocalVersion = SemVer.Parse(Basics.VersionName);

    public void CheckForUpdates()
    {
        if (Basics.DeviceArchitecture is Architecture.X86 or Architecture.Arm) //32 位系统
        {
            LogWrapper.Warn("Update", "Current device architecture is not supported, ignore any updates.");
            return;
        }
        // Mirror 酱的
        if (_MirrorChyanSource.Abilities.Contains(SourceAbility.Update))
        {
            if (CheckMirrorChyan()) return;
        }

        // 社区公益服务器的
        CheckCommunity();
    }

    private bool CheckCommunity()
    {
        var updateChannelNum = Setup.System.UpdateBranch;
        if (updateChannelNum >= _UpdateBranchString.Length || updateChannelNum < 0)
        {
            LogWrapper.Warn("Update", "Incorrect update branch number, reset to default");
            Setup.System.UpdateBranch = 0;
        }

        if (!string.IsNullOrWhiteSpace(Setup.System.MirrorChyanKey))
        {
            LogWrapper.Info("Update", "Found MirrorChyan key, use MirrorChyan to check update first");
        }
        var updateChannelName =
            $"updates-{_UpdateBranchString[updateChannelNum]}{Basics.DeviceArchitecture.ToString().ToLower()}";
        for (var i = 0; i < _SourceInstance.Length; i++)
        {
            try
            {
                var response = HttpRequestBuilder.Create(_getApiUri(i, "apiv2/cache.json"), HttpMethod.Get)
                    .GetResponse();
                if (!response.IsSuccessStatusCode) continue;
                var responseCtx = response.Content.ReadAsStringAsync().Result;
                if (responseCtx is null) continue;
                var cacheDict = JsonSerializer.Deserialize<Dictionary<string, string>>(responseCtx);
                if (cacheDict is null)
                {
                    LogWrapper.Error("Update", "Failed to deserialize cache data");
                    continue;
                }
                if (cacheDict.TryGetValue(updateChannelName, out var channelHash))
                {

                }
                else
                {
                    LogWrapper.Warn("Update", $"No suitable update channel found for {updateChannelName}");
                    continue;
                }
            }
            catch (Exception e)
            {
                LogWrapper.Warn(e, "Update", $"Failed to get update data from remote server({i})");
            }
        }
        LogWrapper.Error("Update", "No available update sources, unable to check if there is any new versions");
        return false;
    }

    /// <summary>
    /// Get the corresponding url from the given argument
    /// </summary>
    /// <param name="index">The index of the update server</param>
    /// <param name="path">The relative path of the resource</param>
    /// <returns>The url direct to the resource you want</returns>
    /// <exception cref="ArgumentException">The given arguments were illegal</exception>
    private string _getApiUri(int index, string path)
    {
        if (index < 0 || index >= _SourceInstance.Length)
            throw new ArgumentException($"Unexpected argument was given: {nameof(index)}");
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException($"Incorrect path: {nameof(path)}");
        var baseUri = _SourceInstance[index].BaseUrl;
        if (path.StartsWith("/"))
            baseUri = baseUri.TrimEnd("/".ToCharArray());
        return $"{baseUri}{path}";
    }

    private bool CheckMirrorChyan()
    {
        try
        {
            var response = HttpRequestBuilder.Create(_MirrorChyanSource.BaseUrl, HttpMethod.Get)
                .GetResponse();
            if (!response.IsSuccessStatusCode) return false;
            var responseData =
                JsonSerializer.Deserialize<MirrorChyanResponse>(response.Content.ReadAsStringAsync().Result);
            if (responseData is null || responseData.Code != 0 || responseData.Data is null) return false;
            var remoteVersion = SemVer.Parse(responseData.Data.VersionName);
            if (remoteVersion > _LocalVersion)
            {

            }
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "Update", "Failed to get MirrorChyan data");
        }
        return false;
    }
}