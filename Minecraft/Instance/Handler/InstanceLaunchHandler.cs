using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Minecraft.Instance.Handler;

public class InstanceLaunchHandler(string pathRef, string nameRef, JsonObject? versionJsonRef) {
    private string Path => pathRef;
    private string Name => nameRef;
    private JsonObject? VersionJson => versionJsonRef;

    /// <summary>
    /// Retrieves download information for the vanilla main JAR file of a specific Minecraft version.
    /// Requires the corresponding dependency instance to exist.
    /// Throws an exception on failure, returns null if no download is needed.
    /// </summary>
    public async Task<DownloadItem?> CheckClientJarAsync(bool returnNullOnFileUsable) {
        // Check if JSON is valid
        if (VersionJson?["downloads"]?["client"]?["url"] is null) {
            throw new Exception($"Base instance {Name} lacks JAR file download information");
        }

        // Check file
        var checkRes = await Files.CheckAsync(
            System.IO.Path.Combine(Path, $"{Name}.jar"),
            minSize: 1024,
            actualSize: VersionJson!["downloads"]!["client"]!.AsObject().TryGetPropertyValue("size", out var sizeNode) 
                ? Int32.TryParse(sizeNode!.ToString(), out var size) ? size : -1 
                : -1,
            hash: VersionJson["downloads"]!["client"]!["sha1"]?.ToString()
            );

        if (returnNullOnFileUsable && checkRes is null) {
            return null; // File passed validation
        }

        // Return download information
        var jarUrl = VersionJson["downloads"]!["client"]!["url"]!.ToString();
        return new DownloadItem(DlSourceLauncherOrMetaGet(jarUrl), $"{version.Path}{version.Name}.jar", checker);
    }
}
