using System;
using System.Collections.Generic;
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

            if (Setup.System.Debug.AddRandomDelay) {
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

    private static void SortInstance() {
        var groupedInstances = Setup.Ui.DetailedInstanceClassification
            ? GroupAndSortWithDetailedClassification()
            : GroupAndSortWithoutDetailedClassification();

        McInstanceUiDict = groupedInstances
            .OrderBy(g => Array.IndexOf(SortableTypes, g.Key))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // 需要排序的 McInstanceCardType
    private static readonly McInstanceCardType[] SortableTypes = [
        McInstanceCardType.Star, McInstanceCardType.Custom,
        McInstanceCardType.Modded, McInstanceCardType.NeoForge, McInstanceCardType.Fabric,
        McInstanceCardType.Forge, McInstanceCardType.Quilt, McInstanceCardType.LegacyFabric,
        McInstanceCardType.Cleanroom, McInstanceCardType.LiteLoader, McInstanceCardType.Client,
        McInstanceCardType.OptiFine, McInstanceCardType.LabyMod, McInstanceCardType.Release,
        McInstanceCardType.Snapshot, McInstanceCardType.Fool, McInstanceCardType.Old,
        McInstanceCardType.UnknownPatchers
    ];

    // PatcherId 映射
    private static readonly Dictionary<McInstanceCardType, string> PatcherIds = new()
    {
        { McInstanceCardType.Release, "game" },
        { McInstanceCardType.Snapshot, "game" },
        { McInstanceCardType.Fool, "game" },
        { McInstanceCardType.Old, "game" },
        { McInstanceCardType.Star, "game" },
        { McInstanceCardType.Custom, "game" },
        { McInstanceCardType.UnknownPatchers, "game" },
        { McInstanceCardType.NeoForge, "NeoForge" },
        { McInstanceCardType.Fabric, "Fabric" },
        { McInstanceCardType.Forge, "Forge" },
        { McInstanceCardType.Quilt, "Quilt" },
        { McInstanceCardType.LegacyFabric, "LegacyFabric" },
        { McInstanceCardType.Cleanroom, "Cleanroom" },
        { McInstanceCardType.LiteLoader, "LiteLoader" },
        { McInstanceCardType.OptiFine, "OptiFine" },
        { McInstanceCardType.LabyMod, "LabyMod" },
        { McInstanceCardType.Modded, "Modded" },
        { McInstanceCardType.Client, "Client" }
    };

    private static IEnumerable<IGrouping<McInstanceCardType, McInstance>> GroupAndSortWithDetailedClassification()
    {
        var moddedTypes = new[] { McInstanceCardType.NeoForge, McInstanceCardType.Fabric, McInstanceCardType.Forge,
                                  McInstanceCardType.Quilt, McInstanceCardType.LegacyFabric,
                                  McInstanceCardType.Cleanroom, McInstanceCardType.LiteLoader };
        var clientTypes = new[] { McInstanceCardType.OptiFine, McInstanceCardType.LabyMod };

        // 按类型分组并内部排序
        var sortedGroups = new List<IGrouping<McInstanceCardType, McInstance>>();
        foreach (var type in SortableTypes)
        {
            var instances = McInstanceList
                .Where(instance => !IsIgnoredType(instance.GetInstanceDisplayType()))
                .Where(instance => type switch
                {
                    McInstanceCardType.Modded => moddedTypes.Any(t => instance.GetVersionInfo()!.GetPatcher(PatcherIds[t]) != null),
                    McInstanceCardType.Client => clientTypes.Any(t => instance.GetVersionInfo()!.GetPatcher(PatcherIds[t]) != null),
                    _ => instance.GetInstanceDisplayType() == type
                })
                .OrderBy(instance => GetSortKeyAndComparer(instance, type)
                , Comparer<object>.Create((x, y) =>
                    x is DateTime dtX && y is DateTime dtY ? DateTime.Compare(dtX, dtY) : 
                        McVersionComparerFactory.GetComparer(type).Compare(x.ToString(), y.ToString())))
                .ToList();

            if (instances.Count > 0)
                sortedGroups.Add(new Grouping(type, instances));
        }

        // 合并 Modded 和 Client
        var moddedGroup = sortedGroups
            .Where(g => moddedTypes.Contains(g.Key))
            .SelectMany(g => g)
            .OrderBy(instance =>
            {
                foreach (var t in moddedTypes)
                {
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
            .OrderBy(instance =>
            {
                foreach (var t in clientTypes)
                {
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
        if (moddedGroup.Any())
            sortedGroups.Add(new Grouping(McInstanceCardType.Modded, moddedGroup));
        if (clientGroup.Any())
            sortedGroups.Add(new Grouping(McInstanceCardType.Client, clientGroup));

        return sortedGroups;
    }

    private static IEnumerable<IGrouping<McInstanceCardType, McInstance>> GroupAndSortWithoutDetailedClassification()
    {
        return McInstanceList
            .Where(instance => !IsIgnoredType(instance.GetInstanceDisplayType()))
            .GroupBy(instance => instance.GetInstanceDisplayType())
            .Select(g =>
            {
                return new Grouping(g.Key, g.OrderBy(instance => GetSortKeyAndComparer(instance, g.Key)
                        , Comparer<object>.Create((x, y) =>
                        x is DateTime dtX && y is DateTime dtY ? DateTime.Compare(dtX, dtY) : 
                            McVersionComparerFactory.GetComparer(g.Key).Compare(x.ToString(), y.ToString())))
                    .ToList());
            });
    }

    private static bool IsIgnoredType(McInstanceCardType type) => type == McInstanceCardType.Error;

    private static object GetSortKeyAndComparer(McInstance instance, McInstanceCardType type)
    {
        var comparer = McVersionComparerFactory.GetComparer(type);
        if (comparer is ReleaseTimeComparer) {
            return instance.GetVersionInfo()!.ReleaseTime;
        }

        var patcherId = PatcherIds[type];
        var patcher = instance.GetVersionInfo()!.GetPatcher(patcherId);
        return patcher?.Version ?? instance.GetVersionInfo()!.McVersion;
    }

    // 辅助类实现 IGrouping
    private class Grouping : IGrouping<McInstanceCardType, McInstance>
    {
        public McInstanceCardType Key { get; }
        private readonly IEnumerable<McInstance> _elements;

        public Grouping(McInstanceCardType key, IEnumerable<McInstance> elements)
        {
            Key = key;
            _elements = elements;
        }

        public IEnumerator<McInstance> GetEnumerator() => _elements.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
