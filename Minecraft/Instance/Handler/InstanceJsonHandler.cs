using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Instance.Handler;

public class InstanceJsonHandler(string pathRef, string nameRef) {
    private string Path => pathRef;
    private string Name => nameRef;

    public async Task<JsonObject?> RefreshVersionJsonAsync() {
        var jsonPath = System.IO.Path.Combine(Path, $"{Name}.json");
        if (!File.Exists(jsonPath)) {
            var jsonFiles = Directory.GetFiles(Path, "*.json");
            if (jsonFiles.Length == 1) {
                jsonPath = jsonFiles[0];
            } else {
                return null;
            }
        }

        try {
            // 异步读取文件内容
            await using var fileStream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var jsonNode = await JsonNode.ParseAsync(fileStream);
            var jsonObject = jsonNode!.AsObject();

            return jsonObject; // 保存到字段
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"初始化实例 JSON 失败（{Name}）");
        }

        return null;
    }
    
    /// <summary>
    /// 实例 JAR 中的 version.json 文件对象
    /// </summary>
    public async Task<JsonObject?> RefreshVersionJsonInJarAsync() {
        var jarPath = $"{Path}{Name}.jar";
        if (!File.Exists(jarPath)) {
            return null;
        }

        try {
            await using var fileStream = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var zipFile = new ZipFile(fileStream); // SharpZipLib 的 ZipFile

            var versionJsonEntry = zipFile.GetEntry("version.json");
            if (versionJsonEntry != null) {
                await using var entryStream = zipFile.GetInputStream(versionJsonEntry);
                var jsonNode = await JsonNode.ParseAsync(entryStream);
                if (jsonNode is JsonObject jsonObj) {
                    return jsonObj;
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "从实例 JAR 中读取 version.json 失败");
        }
        return null;
    }
}
