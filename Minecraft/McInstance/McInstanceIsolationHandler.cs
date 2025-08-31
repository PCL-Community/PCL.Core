using System;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public class McInstanceIsolationHandler {
    public string GetInstancePath(McInstance instance) {
        var path = instance.Path;
        var name = instance.Name;
        var state = instance.State;

        if (SetupService.IsUnset(SetupEntries.Instance.IndieV2, path)) {
            if (!instance.IsLoaded) instance.Load();

            bool ShouldBeIndie() {
                // 从旧的实例独立设置迁移
                if (!SetupService.IsUnset(SetupEntries.Instance.IndieV1, path) && SetupService.GetInt32(SetupEntries.Instance.IndieV1, path) > 0) {
                    LogWrapper.Info($"[Minecraft] 版本隔离初始化（{name}）：从旧设置迁移");
                    return SetupService.GetInt32(SetupEntries.Instance.IndieV1, path) == 1;
                }

                // 若存在 mods 或 saves 文件夹，自动开启隔离
                var modFolder = new DirectoryInfo(path + "mods\\");
                var saveFolder = new DirectoryInfo(path + "saves\\");
                if ((modFolder.Exists && modFolder.EnumerateFiles().Any()) ||
                    (saveFolder.Exists && saveFolder.EnumerateDirectories().Any())) {
                    LogWrapper.Info($"[Minecraft] 版本隔离初始化（{name}）：存在 mods 或 saves 文件夹，自动开启");
                    return true;
                }

                // 根据全局设置决定
                bool isRelease = state != McInstanceState.Fool && state != McInstanceState.Old && state != McInstanceState.Snapshot;
                LogWrapper.Info($"[Minecraft] 版本隔离初始化（{name}）：全局设置（{SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2)}），State {state}, IsRelease {isRelease}, Modable {Modable}");

                return SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2) switch {
                    0 => false,
                    1 => instance.Version.HasLabyMod || instance.Modable,
                    2 => !isRelease,
                    3 => instance.Version.HasLabyMod || instance.Modable || !isRelease,
                    _ => true
                };
            }

            SetupService.SetBool(SetupEntries.Instance.IndieV2, ShouldBeIndie(), path);
        }
        return SetupService.GetBool(SetupEntries.Instance.IndieV2) ? path : McFolderManager.PathMcFolder;
    }
}