using PCL.Core.App;
using PCL.Core.Minecraft.Instance;

namespace PCL.Core.Minecraft.Launch.Services;

public static class FileCompleteService {
    public static void FileComplete(McInstance instance, bool checkAssetHash, AssetsIndexExistsBehaviour assetsIndexBehaviour) {
        
    }

    private static bool ShouldIgnoreFileCheck(string path) {
        return Config.Instance.DisableAssetVerifyV2[path];
    }
}

public enum AssetsIndexExistsBehaviour {
    /// <summary>
    /// 如果文件存在，则不进行下载。
    /// </summary>
    DontDownload,
    
    /// <summary>
    /// 如果文件存在，则启动新的下载加载器进行独立的更新。
    /// </summary>
    DownloadInBackground,
    
    /// <summary>
    /// 如果文件存在，也同样进行下载。
    /// </summary>
    AlwaysDownload
}
