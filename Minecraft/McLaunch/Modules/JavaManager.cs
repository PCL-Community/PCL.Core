using System.Threading.Tasks;
using PCL.Core.Minecraft.McLaunch.Services;
using PCL.Core.Minecraft.McLaunch.Services.Java;
using PCL.Core.Minecraft.McLaunch.State;

namespace PCL.Core.Minecraft.McLaunch.Modules;

public static class JavaManager
{
    public static async Task<Result<Java>> SelectJavaAsync()
    {
        var selector = JavaServiceFactory.CreateVersionSelector();
        return await selector.SelectBestJavaAsync();
    }
}
