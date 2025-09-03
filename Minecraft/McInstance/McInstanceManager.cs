using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McInstance;

public static class MinecraftInstanceManager {
    private static McInstance? _mcInstanceCurrent;
    private static object? _mcInstanceLast;

    /// <summary>
    /// List of current Minecraft folders.
    /// </summary>
    public static List<McInstance> McInstanceList { get; } = [];
    
    /// <summary>
    /// 用作 UI 显示被排序过的实例字典
    /// </summary>
    public static Dictionary<McInstanceCardType, McInstance> McInstanceUiDict { get; set; } = [];

    /// <summary>
    /// 当前的 Minecraft 实例
    /// </summary>
    public static McInstance? McInstanceCurrent {
        get => _mcInstanceCurrent;
        set {
            if (ReferenceEquals(_mcInstanceLast, value)) return;
            _mcInstanceCurrent = value;
            _mcInstanceLast = value;
            if (value == null) return;
        }
    }

    public static async Task McInstanceListLoadAsync(string path, CancellationToken cancelToken = default) {
        try {
            // Get version folders
            var versionPath = Path.Combine(path, "versions");

            await Directories.CheckPermissionWithExceptionAsync(versionPath, cancelToken);
            foreach (var instance in Directory.GetDirectories(versionPath)) {
                var mcInstance = new McInstance(instance);
                await mcInstance.Check();
                McInstanceList.Add(mcInstance);
            }

            SelectInstanceAsync(McInstanceList, cancelToken);

            if (Setup.System.Debug.AddRandomDelay) {
                await Task.Delay(Random.Shared.Next(200, 3000), cancelToken);
            }
        } catch (OperationCanceledException) {
            // Handle cancellation
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "加载 Minecraft 实例列表失败");
        }
    }

    private static void SelectInstanceAsync(List<McInstance> path, CancellationToken cancellationToken) {
        var savedSelection = Setup.Launch.SelectedInstance;

        if (McInstanceList.Any(kvp => kvp.GetInstanceDisplayType() != McInstanceCardType.Error)) {
            var selectedInstance = McInstanceList
                .FirstOrDefault(instance => instance.Name == savedSelection && instance.GetInstanceDisplayType() != McInstanceCardType.Error);

            if (selectedInstance != null) {
                McInstanceCurrent = selectedInstance;
                LogWrapper.Warn($"选择保存的 Minecraft 实例：{McInstanceCurrent.Path}");
            } else {
                selectedInstance = McInstanceList
                    .FirstOrDefault(instance => instance.GetInstanceDisplayType() != McInstanceCardType.Error);

                if (selectedInstance != null) {
                    McInstanceCurrent = selectedInstance;
                    Setup.Launch.SelectedInstance = McInstanceCurrent.Name;
                    LogWrapper.Warn($"自动选择 Minecraft 实例：{McInstanceCurrent.Path}");
                } else {
                    McInstanceCurrent = null;
                    LogWrapper.Warn("未找到可用的 Minecraft 实例");
                }
            }
        } else {
            McInstanceCurrent = null;
            if (savedSelection != null) {
                Setup.Launch.SelectedInstance = null;
                LogWrapper.Warn("清除失效的 Minecraft 实例选择");
            }
            LogWrapper.Warn("未找到可用的 Minecraft 实例");
        }
    }
}
