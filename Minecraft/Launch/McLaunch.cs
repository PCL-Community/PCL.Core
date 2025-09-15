using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Minecraft.Launch;

public static class McLaunch {
    public static async Task<bool> LaunchInstanceAsync() {
        var launchCts = new CancellationTokenSource();

        try {
            return true;
        } finally {
            launchCts.Dispose(); // 确保在pipeline执行完成后释放
        }
    }
}
