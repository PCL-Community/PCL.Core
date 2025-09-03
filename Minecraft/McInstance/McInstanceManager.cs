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
        McInstanceUiDict = McInstanceList
            .ToDictionary(
                instance => instance.GetInstanceDisplayType(),
                instance => instance
            )
            .OrderBy(kvp => (int)kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /*
        // 常规实例：快照放在最上面，此后按版本号从高到低排序
        if (McInstanceUiDict.ContainsKey(McInstanceCardType.Release))
        {
            List<McInstance> OldList = ResultInstanceList[McInstanceCardType.OriginalLike];
            // 提取快照
            McInstance Snapshot = null;
            foreach (McInstance Instance in OldList) {
                if (Instance.State == McInstanceState.Snapshot) {
                    Snapshot = Instance;
                    break;
                }
            }
            if (Snapshot != null) {
                OldList.Remove(Snapshot);
            }
            // 按版本号排序
            List<McInstance> NewList = OldList.OrderByDescending(v => v.Version.McCodeMain).ToList();
            // 回设
            if (Snapshot != null) {
                NewList.Insert(0, Snapshot);
            }
            ResultInstanceList[McInstanceCardType.OriginalLike] = NewList;
        }

        // 不常用实例：按发布时间新旧排序，如果不可用则按名称排序
        if (ResultInstanceList.ContainsKey(McInstanceCardType.Rubbish))
        {
            ResultInstanceList[McInstanceCardType.Rubbish].Sort((Left, Right) => {
                int LeftYear = Left.ReleaseTime.Year; // + (Left.State == McInstanceState.Original || Left.Version.HasOptiFine ? 100 : 0);
                int RightYear = Right.ReleaseTime.Year; // + (Right.State == McInstanceState.Original || Right.Version.HasOptiFine ? 100 : 0);
                if (LeftYear > 2000 && RightYear > 2000) {
                    if (LeftYear != RightYear) {
                        return LeftYear > RightYear ? 1 : -1;
                    } else {
                        return Left.ReleaseTime > Right.ReleaseTime ? 1 : -1;
                    }
                } else if (LeftYear > 2000 && RightYear < 2000) {
                    return 1;
                } else if (LeftYear < 2000 && RightYear > 2000) {
                    return -1;
                } else {
                    return string.Compare(Left.Name, Right.Name);
                }
            });
        }

        // API 实例：优先按版本排序，此后【先放 Fabric / Quilt / Legacy Fabric，再放 Neo/Forge（按版本号从高到低排序），然后放 Cleanroom / LabyMod，最后放 LiteLoader（按名称排序）】
        if (ResultInstanceList.ContainsKey(McInstanceCardType.API)) {
            ResultInstanceList[McInstanceCardType.API].Sort((Left, Right) => {
                int Basic = VersionSortInteger(Left.Version.McName, Right.Version.McName);
                if (Basic != 0) {
                    return Basic > 0 ? 1 : -1;
                } else {
                    if (Left.Version.HasFabric != Right.Version.HasFabric) {
                        return Left.Version.HasFabric ? 1 : -1;
                    } else if (Left.Version.HasQuilt != Right.Version.HasQuilt) {
                        return Left.Version.HasQuilt ? 1 : -1;
                    } else if (Left.Version.HasLegacyFabric != Right.Version.HasLegacyFabric) {
                        return Left.Version.HasLegacyFabric ? 1 : -1;
                    } else if (Left.Version.HasNeoForge != Right.Version.HasNeoForge) {
                        return Left.Version.HasNeoForge ? 1 : -1;
                    } else if (Left.Version.HasForge != Right.Version.HasForge) {
                        return Left.Version.HasForge ? 1 : -1;
                    } else if (Left.Version.HasCleanroom != Right.Version.HasCleanroom) {
                        return Left.Version.HasCleanroom ? 1 : -1;
                    } else if (Left.Version.HasLabyMod != Right.Version.HasLabyMod) {
                        return Left.Version.HasLabyMod ? 1 : -1;
                    } else if (Left.Version.SortCode != Right.Version.SortCode) {
                        return Left.Version.SortCode > Right.Version.SortCode ? 1 : -1;
                    } else {
                        return string.Compare(Left.Name, Right.Name);
                    }
                }
            });
        }*/
    }
}
