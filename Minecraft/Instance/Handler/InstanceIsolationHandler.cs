using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Folder;

namespace PCL.Core.Minecraft.Instance.Handler;

public class InstanceIsolationHandler(string pathRef, string nameRef, McInstanceCardType? cardTypeRef, McInstanceInfo? instanceInfoRef) {
    private string Path => pathRef;
    private string Name => nameRef;
    private McInstanceCardType? CardType => cardTypeRef;
    private McInstanceInfo? InstanceInfo => instanceInfoRef;
    
    /// <summary>
    /// 获取实例的隔离路径，根据全局设置和实例特性决定是否使用独立文件夹。
    /// </summary>
    /// <returns>隔离后的路径，以“\”结尾</returns>
    public string? GetIsolatedPath() {
        if (CardType == McInstanceCardType.Error) {
            return null;
        }
        
        if (!Config.Instance.IndieV2[Path]) {
            var shouldBeIndie = ShouldBeIndie();
            Config.Instance.IndieV2[Path] = shouldBeIndie;
        }
        return Config.Instance.IndieV2[Path] ? Path : McFolderManager.PathMcFolder;
    }

    private bool ShouldBeIndie() {
        // 若存在 mods 或 saves 文件夹，自动开启隔离
        var modFolder = new DirectoryInfo(Path + "mods\\");
        var saveFolder = new DirectoryInfo(Path + "saves\\");
        if ((modFolder.Exists && modFolder.EnumerateFiles().Any()) ||
            (saveFolder.Exists && saveFolder.EnumerateDirectories().Any())) {
            LogWrapper.Info("Isolation", $"版本隔离初始化（{Name}）：存在 mods 或 saves 文件夹，自动开启");
            return true;
        }

        var isModded = InstanceInfo!.IsModded;
        var isRelease = InstanceInfo.VersionType == McVersionType.Release;
        LogWrapper.Info("Isolation", $"版本隔离初始化({Name}): 全局设置({Config.Launch.IndieSolutionV2})");
        
        return Config.Launch.IndieSolutionV2 switch {
            0 => false,
            1 => InstanceInfo.HasPatcher("labymod") || isModded,
            2 => !isRelease,
            3 => InstanceInfo.HasPatcher("labymod") || isModded || !isRelease,
            _ => true
        };
    }
}
