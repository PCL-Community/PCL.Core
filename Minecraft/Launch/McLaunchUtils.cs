using PCL.Core.Logging;

namespace PCL.Core.Minecraft.Launch;

public static class McLaunchUtils {
    public static void Log(string msg) {
        // TODO: UI Log
        LogWrapper.Info("McLaunch", msg);
    }
}
