using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft;

public class ClientListManager {
    private static async Task<DlClientListResult> DlClientListMojangMainAsync(TaskBase<DlClientListResult> loader, params object[] args) {
        var input = args.Length > 0 ? (string) args[0] : string.Empty;
        var startTime = TimeUtils.GetTimeTick();

        try {
            using var mojangSourceResponse = await HttpRequestBuilder
                .Create("https://launchermeta.mojang.com/mc/game/version_manifest.json", HttpMethod.Get)
                .SendAsync();
            if (!mojangSourceResponse.IsSuccess) throw new InvalidOperationException("获取官方源版本列表失败");
            
            var jsonObject = await mojangSourceResponse.AsJsonAsync<JsonObject>();
            if (jsonObject == null) {
                throw new InvalidOperationException("获取到的官方源版本列表 Json 格式错误");
            }
            var versions = jsonObject["versions"]!.AsArray();
            if (versions.Count < 200) {
                throw new Exception($"获取到的版本列表长度不足（{jsonObject})");
            }

            // 添加 UVMC 项
            var cacheFilePath = Path.Combine(FileService.TempPath, "Cache", "uvmc-download.json");
            if (!File.Exists(cacheFilePath)) {
                try {
                    using var uvmcSourceResponse = await HttpRequestBuilder.Create(
                        "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto/version_manifest.json", HttpMethod.Get)
                        .SendAsync();
                    if (!uvmcSourceResponse.IsSuccess) throw new InvalidOperationException("获取 UVMC 源版本列表失败");
                    
                    var unlistedJsonObject = await uvmcSourceResponse.AsJsonAsync<JsonObject>();
                    if (unlistedJsonObject == null) {
                        throw new InvalidOperationException("获取到的 UVMC 源版本列表 Json 格式错误");
                    }
                    await Files.WriteFileAsync(cacheFilePath, await uvmcSourceResponse.AsStreamAsync());
                } catch (Exception ex) {
                    LogWrapper.Warn(ex, "[Download] 未列出的版本官方源下载失败");
                }
            }

            try {
                JObject cachedJson = GetJson(File.ReadAllText(cacheFilePath));
                versions.Merge(cachedJson["versions"]);
            } catch (Exception ex) {
                Log(ex, "[Download] UVMC 列表加载失败，忽略列表内容");
            }

            // 确定官方源是否可用
            if (!DlPreferMojang) {
                long deltaTime = TimeUtils.GetTimeTick() - startTime;
                DlPreferMojang = deltaTime < 4000;
                Log($"[Download] Mojang 官方源加载耗时：{deltaTime}ms，{(DlPreferMojang ? "可优先使用官方源" : "不优先使用官方源")}");
            }

            // 返回结果
            var result = new DlClientListResult { IsOfficial = true, SourceName = "Mojang 官方源", Value = json };
            loader.Result = result;

            // 解析更新提示（Release）
            string? version = json["latest"]?["release"]?.ToString();
            if (Setup.Get("ToolUpdateRelease") &&
                !string.IsNullOrEmpty(Setup.Get("ToolUpdateReleaseLast")) &&
                version != null &&
                Setup.Get("ToolUpdateReleaseLast") != version) {
                McDownloadClientUpdateHint(version, json);
                IsNewClientVersionHinted = true;
            }
            McVersionHighest = version?.Split('.')[1] ?? string.Empty;
            Setup.Set("ToolUpdateReleaseLast", version ?? string.Empty);

            // 解析更新提示（Snapshot）
            version = json["latest"]?["snapshot"]?.ToString();
            if (Setup.Get("ToolUpdateSnapshot") &&
                !string.IsNullOrEmpty(Setup.Get("ToolUpdateSnapshotLast")) &&
                version != null &&
                Setup.Get("ToolUpdateSnapshotLast") != version &&
                !IsNewClientVersionHinted) {
                McDownloadClientUpdateHint(version, json);
            }
            Setup.Set("ToolUpdateSnapshotLast", version ?? "Nothing");

            return result;
        } catch (Exception ex) {
            throw new Exception("Minecraft 官方源版本列表解析失败", ex);
        }
    }

    // BMCLAPI 源加载器
    public static TaskBase<DlClientListResult> DlClientListBmclapiLoader { get; } =
        new TaskBase<DlClientListResult>("DlClientList Bmclapi", DlClientListBmclapiMainAsync);

    private static async Task<DlClientListResult> DlClientListBmclapiMainAsync(TaskBase<DlClientListResult> loader, params object[] args) {
        string input = args.Length > 0 ? (string)args[0] : string.Empty;

        try {
            JObject json = await NetGetCodeByRequestRetry("https://bmclapi2.bangbang93.com/mc/game/version_manifest.json", isJson: true);
            JArray versions = (JArray)json["versions"]!;
            if (versions.Count < 200)
                throw new Exception($"获取到的版本列表长度不足（{json})");

            // 添加 UVMC 项
            string cacheFilePath = Path.Combine(PathTemp, "Cache", "uvmc-download.json");
            if (!File.Exists(cacheFilePath)) {
                try {
                    JObject unlistedJson = await NetGetCodeByRequestRetry(
                        "https://alist.8mi.tech/d/mirror/unlisted-versions-of-minecraft/Auto/version_manifest.json",
                        isJson: true);
                    File.WriteAllText(cacheFilePath, unlistedJson.ToString());
                } catch (Exception ex) {
                    Log($"[Download] 未列出的版本镜像源下载失败: {ex.Message}");
                }
            }

            try {
                JObject cachedJson = GetJson(File.ReadAllText(cacheFilePath));
                versions.Merge(cachedJson["versions"]);
            } catch (Exception ex) {
                Log(ex, "[Download] UVMC 列表加载失败，忽略列表内容");
            }

            // 检查是否有要求的版本
            if (!string.IsNullOrEmpty(input)) {
                if (DlClientListLoader?.Result?.Value?["versions"] is JArray mainVersions &&
                    !mainVersions.Any(v => v["id"]?.ToString() == input)) {
                    throw new Exception($"BMCLAPI 源未包含目标版本 {input}");
                }
            }

            // 返回结果
            var result = new DlClientListResult { IsOfficial = false, SourceName = "BMCLAPI", Value = json };
            loader.Result = result;
            return result;
        } catch (Exception ex) {
            throw new Exception($"Minecraft BMCLAPI 版本列表解析失败（{json})", ex);
        }
    }
}
