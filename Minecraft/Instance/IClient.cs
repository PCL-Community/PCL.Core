using System.Threading.Tasks;
using PCL.Core.Net;
using System.Text.Json.Nodes;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.Instance;

public interface IClient
{
    static abstract Task<JsonNode?> GetVersionInfoAsync(string version);
    static abstract Task UpdateVersionIndexAsync();
    static abstract List<DownloadItem> AnalysisLibrary(JsonNode versionJson);
    static abstract Task<string> GetJsonAsync(string version,string exceptHash);
}
