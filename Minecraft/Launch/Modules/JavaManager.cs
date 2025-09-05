using System.Threading.Tasks;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Modules;

public static class JavaManager
{
    public static async Task<Result<JavaInfo>> SelectJavaAsync()
    {
        var selector = JavaServiceFactory.CreateVersionSelector();
        return await selector.SelectBestJavaAsync();
    }
}
