using System;
using System.Threading.Tasks;
using PCL.Core.Minecraft.McLaunch.State;

namespace PCL.Core.Minecraft.McLaunch;

public static class McLauncher
{
    public static async Task<bool> LaunchAsync(LaunchOptions options = null)
    {
        try
        {
            var config = LaunchConfiguration.Load();
            var result = await LaunchOrchestrator.LaunchMinecraftAsync(options ?? new LaunchOptions());
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            LaunchEventManager.RaiseError(ex);
            return false;
        }
    }
}

