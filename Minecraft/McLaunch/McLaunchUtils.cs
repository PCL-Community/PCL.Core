using PCL.Core.Logging;

namespace PCL.Core.Minecraft.McLaunch;

public static class McLaunchUtils {
    public static void Log(string msg) {
        // TODO: UI Log
        LogWrapper.Info("McLaunch", msg);
    }
}
