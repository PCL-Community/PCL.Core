using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Tasks;
using PCL.Core.Minecraft.Launch.Services;
using PCL.Core.Minecraft.Launch.State;

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
