using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Instance.Interface;

public interface IMcInstanceJson {
    Task<JsonObject?> GetVersionJsonAsync();
    
    Task<JsonObject?> RefreshVersionJsonAsync();
    
    Task<JsonObject?> GetVersionJsonInJarAsync();
    
    Task<JsonObject?> RefreshVersionJsonInJarAsync();
}
