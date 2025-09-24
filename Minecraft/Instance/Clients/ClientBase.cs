using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.IO;

namespace PCL.Core.Minecraft.Instance.Clients;

public class ClientBase : IClient
{
    
    public static Task<JsonNode?> GetVersionInfoAsync(string version)
    {
        throw new NotImplementedException();
    }
    
    public static Task ParseAsync(string version)
    {
        throw new NotImplementedException();
    }
    
    public static Task UpdateVersionIndexAsync()
    {
        throw new NotImplementedException();
    }
    
    public virtual Task<IEnumerable<NetFile>> AnalyzeLibraryAsync()
    {
        throw new NotImplementedException();
    }
    
    public virtual Task<IEnumerable<NetFile>> AnalyzeMissingLibraryAsync()
    {
        throw new NotImplementedException();
    }

    public virtual Task ExecuteInstallerAsync(string path)
    {
        throw new NotImplementedException();
    }
    
    public virtual Task<string> GetJsonAsync()
    {
        throw new NotImplementedException();
    }
}