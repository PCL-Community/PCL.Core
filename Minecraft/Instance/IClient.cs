using System.Threading.Tasks;
using PCL.Core.Net;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using PCL.Core.IO;

namespace PCL.Core.Minecraft.Instance;

public interface IClient
{
    static abstract Task<JsonNode?> GetVersionInfoAsync(string version);
    static abstract Task UpdateVersionIndexAsync();
    abstract Task<IEnumerable<NetFile>> AnalyzeLibraryAsync();
    abstract Task<string> GetJsonAsync();
    static abstract Task ParseAsync(string version);
    abstract Task ExecuteInstallerAsync(string path);
    abstract Task<IEnumerable<NetFile>> AnalyzeMissingLibraryAsync();
}
