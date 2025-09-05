using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

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
    public static Dictionary<McInstanceCardType, List<McInstance>> McInstanceUiDict { get; set; } = [];

    /// <summary>
    /// 当前的 Minecraft 实例
    /// </summary>
    public static McInstance? McInstanceCurrent {
        get => _mcInstanceCurrent;
        set {
            if (ReferenceEquals(_mcInstanceLast, value)) return;
            _mcInstanceCurrent = value;
            _mcInstanceLast = value;
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

            SelectInstanceAsync();

            if (Config.System.Debug.AddRandomDelay) {
                await Task.Delay(Random.Shared.Next(200, 3000), cancelToken);
            }
        } catch (OperationCanceledException) {
            // Handle cancellation
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "加载 Minecraft 实例列表失败");
        }

        SortInstance();

        foreach (var instance in McInstanceList) {
            instance.Load();
        }
    }

    private static void SelectInstanceAsync() {
        var savedSelection = Config.Launch.SelectedInstance;

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
                    Config.Launch.SelectedInstance = McInstanceCurrent.Name;
                    LogWrapper.Warn($"自动选择 Minecraft 实例：{McInstanceCurrent.Path}");
                } else {
                    McInstanceCurrent = null;
                    LogWrapper.Warn("未找到可用的 Minecraft 实例");
                }
            }
        } else {
            McInstanceCurrent = null;
            if (savedSelection.IsNullOrEmpty()) {
                Config.Launch.SelectedInstance = string.Empty;
                LogWrapper.Warn("清除失效的 Minecraft 实例选择");
            }
            LogWrapper.Warn("未找到可用的 Minecraft 实例");
        }
    }

    private static void SortInstance() {
        var groupedInstances = Config.Ui.DetailedInstanceClassification
            ? GroupAndSortWithDetailedClassification()
            : GroupAndSortWithoutDetailedClassification();

        McInstanceUiDict = groupedInstances
            .OrderBy(g => Array.IndexOf(SortableTypes, g.Key))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // 需要排序的 McInstanceCardType
    private static readonly McInstanceCardType[] SortableTypes = [
        // 收藏和自定义分类
        McInstanceCardType.Star, McInstanceCardType.Custom,
        // 模组加载器和细分分类
        McInstanceCardType.Modded, McInstanceCardType.NeoForge, McInstanceCardType.Fabric,
        McInstanceCardType.Forge, McInstanceCardType.Quilt, McInstanceCardType.LegacyFabric,
        McInstanceCardType.Cleanroom, McInstanceCardType.LiteLoader,
        // 客户端和细分分类
        McInstanceCardType.Client,
        McInstanceCardType.OptiFine, McInstanceCardType.LabyMod,
        // 原版版本
        McInstanceCardType.Release, McInstanceCardType.Snapshot,
        McInstanceCardType.Fool, McInstanceCardType.Old,
        // 最低优先级
        McInstanceCardType.Hidden, McInstanceCardType.UnknownPatchers, McInstanceCardType.Error
    ];

    // PatcherId 映射
    private static readonly Dictionary<McInstanceCardType, string> PatcherIds = new() {
        // Game
        { McInstanceCardType.Release, "game" },
        { McInstanceCardType.Snapshot, "game" },
        { McInstanceCardType.Fool, "game" },
        { McInstanceCardType.Old, "game" },
        // Specific
        { McInstanceCardType.Star, "game" },
        { McInstanceCardType.Custom, "game" },
        { McInstanceCardType.Hidden, "game" },
        { McInstanceCardType.UnknownPatchers, "game" },
        // ModLoaders
        { McInstanceCardType.NeoForge, "NeoForge" },
        { McInstanceCardType.Fabric, "Fabric" },
        { McInstanceCardType.Forge, "Forge" },
        { McInstanceCardType.Quilt, "Quilt" },
        { McInstanceCardType.LegacyFabric, "LegacyFabric" },
        { McInstanceCardType.Cleanroom, "Cleanroom" },
        { McInstanceCardType.LiteLoader, "LiteLoader" },
        // Client
        { McInstanceCardType.OptiFine, "OptiFine" },
        { McInstanceCardType.LabyMod, "LabyMod" }
    };

    private static List<IGrouping<McInstanceCardType, McInstance>> GroupAndSortWithoutDetailedClassification() {
        var moddedTypes = new[] {
            McInstanceCardType.NeoForge, McInstanceCardType.Fabric, McInstanceCardType.Forge,
            McInstanceCardType.Quilt, McInstanceCardType.LegacyFabric,
            McInstanceCardType.Cleanroom, McInstanceCardType.LiteLoader
        };
        var clientTypes = new[] { McInstanceCardType.OptiFine, McInstanceCardType.LabyMod };

        // 先按类型分组，保留所有 McInstanceCardType
        var groupedInstances = McInstanceList
            .GroupBy(instance => instance.GetInstanceDisplayType())
            .ToList();

        // 处理每个分组，忽略类型的分组不排序
        var sortedGroups = new List<IGrouping<McInstanceCardType, McInstance>>();
        foreach (var type in SortableTypes) {
            var group = groupedInstances.FirstOrDefault(g => g.Key == type);
            var instances = group?.ToList() ?? [];

            if (instances.Count == 0) {
                continue;
            }
            
            if (!IsIgnoredType(type)) {
                // 对非忽略类型的分组进行排序
                instances = instances
                    .OrderBy(instance => GetSortKey(instance, type),
                        McVersionComparerFactory.PatcherVersionComparer)
                    .ToList();
            }
            sortedGroups.Add(new Grouping(type, instances));
        }

        // 合并 Modded 和 Client
        var moddedGroup = sortedGroups
            .Where(g => moddedTypes.Contains(g.Key))
            .SelectMany(g => g)
            .OrderBy(instance => {
                foreach (var t in moddedTypes) {
                    var patcher = instance.GetVersionInfo()!.GetPatcher(PatcherIds[t]);
                    if (patcher != null)
                        return (Array.IndexOf(SortableTypes, t), patcher.Version);
                }
                return (int.MaxValue, "");
            })
            .ToList();

        var clientGroup = sortedGroups
            .Where(g => clientTypes.Contains(g.Key))
            .SelectMany(g => g)
            .OrderBy(instance => {
                foreach (var t in clientTypes) {
                    var patcher = instance.GetVersionInfo()!.GetPatcher(PatcherIds[t]);
                    if (patcher != null)
                        return (Array.IndexOf(SortableTypes, t), patcher.Version);
                }
                return (int.MaxValue, "");
            })
            .ToList();

        // 过滤掉 Modded 和 Client 相关类型的单独分组
        sortedGroups = sortedGroups
            .Where(g => !moddedTypes.Contains(g.Key) && !clientTypes.Contains(g.Key))
            .ToList();

        // 添加合并后的 Modded 和 Client 分组
        if (moddedGroup.Count > 0)
            sortedGroups.Add(new Grouping(McInstanceCardType.Modded, moddedGroup));
        if (clientGroup.Count > 0)
            sortedGroups.Add(new Grouping(McInstanceCardType.Client, clientGroup));

        return sortedGroups;
    }

    private static IEnumerable<IGrouping<McInstanceCardType, McInstance>> GroupAndSortWithDetailedClassification() {
        return McInstanceList
            .GroupBy(instance => instance.GetInstanceDisplayType()) // 先分组，保留所有 McInstanceCardType
            .Select(g => {
                var instances = g.ToList(); // 转换为 List 以便操作
                if (!IsIgnoredType(g.Key)) {
                    // 对非忽略类型的分组进行排序
                    instances = instances
                        .OrderBy(instance => GetSortKey(instance, g.Key),
                            McVersionComparerFactory.PatcherVersionComparer)
                        .ToList();
                }
                // 返回分组，忽略类型的分组保持原序
                return new Grouping(g.Key, instances);
            });
    }

    private static bool IsIgnoredType(McInstanceCardType type) => type == McInstanceCardType.Error;

    private static (McInstanceCardType, PatcherInfo) GetSortKey(McInstance instance, McInstanceCardType type) {
        var patcherId = PatcherIds[type];
        return (type, instance.GetVersionInfo()!.GetPatcher(patcherId)!);
    }

    // 辅助类实现 IGrouping
    private class Grouping(McInstanceCardType key, IEnumerable<McInstance> elements) : IGrouping<McInstanceCardType, McInstance> {
        public McInstanceCardType Key { get; } = key;

        public IEnumerator<McInstance> GetEnumerator() => elements.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
