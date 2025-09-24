using System.Collections.Generic;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Instance.Clients;

namespace PCL.Core.Minecraft.Instance;

public static class InstanceInstallHandler
{
    public static async Task StartClientInstallAsync(IEnumerable<ClientBase> clients,string path)
    {
        var task = new List<Task>();
        foreach (var client in clients){
            task.Add(client.GetJsonAsync());
        }
    }
    
}