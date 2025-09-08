using System.IO;
using PCL.Core.App;

namespace PCL.Core.Minecraft.Instance.Handler;

public static class InstanceBasicHandler {
    public static string GetName(string path) {
        return string.IsNullOrEmpty(path) ? "" : new DirectoryInfo(path).Name;
    }
    
    public static bool GetIsStarred(string path) {
        return Config.Instance.Starred[path];
    }
}
