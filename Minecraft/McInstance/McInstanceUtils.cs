using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.McInstance;

public static class McInstanceUtils {
    /// <summary>
    /// 获取实例的隔离路径，根据全局设置和实例特性决定是否使用独立文件夹。
    /// </summary>
    /// <param name="instance">Minecraft 实例</param>
    /// <returns>隔离后的路径，以“\”结尾</returns>
    public static async Task<string> GetIsolatedPathAsync(McInstance instance) {
        if (SetupService.IsUnset(SetupEntries.Instance.IndieV2, instance.Path)) {
            bool shouldBeIndie = await ShouldBeIndieAsync(instance);
            SetupService.SetBool(SetupEntries.Instance.IndieV2, shouldBeIndie, instance.Path);
        }
        return SetupService.GetBool(SetupEntries.Instance.IndieV2) ? instance.Path : McFolderManager.PathMcFolder;
    }

    private static async Task<bool> ShouldBeIndieAsync(McInstance instance) {
        // 从旧的实例独立设置迁移
        if (!SetupService.IsUnset(SetupEntries.Instance.IndieV1, instance.Path) && SetupService.GetInt32(SetupEntries.Instance.IndieV1, instance.Path) > 0) {
            LogWrapper.Info($"[Minecraft] 版本隔离初始化（{instance.Name}）：从旧设置迁移");
            return SetupService.GetInt32(SetupEntries.Instance.IndieV1, instance.Path) == 1;
        }

        // 若存在 mods 或 saves 文件夹，自动开启隔离
        var modFolder = new DirectoryInfo(instance.Path + "mods\\");
        var saveFolder = new DirectoryInfo(instance.Path + "saves\\");
        if ((modFolder.Exists && modFolder.EnumerateFiles().Any()) ||
            (saveFolder.Exists && saveFolder.EnumerateDirectories().Any())) {
            LogWrapper.Info($"[Minecraft] 版本隔离初始化（{instance.Name}）：存在 mods 或 saves 文件夹，自动开启");
            return true;
        }

        var isModable = await GetIsModableAsync(instance);
        bool isRelease = instance.State != McInstanceState.Fool && instance.State != McInstanceState.Old && instance.State != McInstanceState.Snapshot;
        LogWrapper.Info($"[Minecraft] 版本隔离初始化（{instance.Name}）：全局设置（{SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2)}），State {instance.State}, IsRelease {isRelease}, Modable {isModable}");

        var version = await instance.GetVersionAsync();
        return SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2) switch {
            0 => false,
            1 => version.HasLabyMod || isModable,
            2 => !isRelease,
            3 => version.HasLabyMod || isModable || !isRelease,
            _ => true
        };
    }

    /// <summary>
    /// 判断实例是否支持 Mod。
    /// </summary>
    /// <param name="instance">Minecraft 实例</param>
    /// <returns>是否支持 Mod</returns>
    public static async Task<bool> GetIsModableAsync(McInstance instance) {
        if (!instance.IsLoaded) await instance.Load();
        var version = await instance.GetVersionAsync();
        return version.HasFabric || version.HasLegacyFabric || version.HasQuilt ||
               version.HasForge || version.HasLiteLoader || version.HasNeoForge ||
               version.HasCleanroom || instance.DisplayType == McInstanceCardType.API;
    }

    /// <summary>
    /// 确定实例的类型（快照、愚人节、老版本等）。
    /// </summary>
    /// <param name="instance">Minecraft 实例</param>
    /// <returns>实例状态</returns>
    public static async Task<McInstanceState> DetermineInstanceTypeAsync(McInstance instance) {
        var version = await instance.GetVersionAsync();
        var versionJson = await instance.GetVersionJsonAsync();
        if (versionJson["type"]?.ToString() == "fool" || !string.IsNullOrEmpty(GetMcFoolName(version.McName))) {
            return McInstanceState.Fool;
        }
        if (version.McName.Contains("w", StringComparison.OrdinalIgnoreCase) ||
            instance.Name.Contains("combat", StringComparison.OrdinalIgnoreCase) ||
            version.McName.Contains("rc", StringComparison.OrdinalIgnoreCase) ||
            version.McName.Contains("pre", StringComparison.OrdinalIgnoreCase) ||
            version.McName.Contains("experimental", StringComparison.OrdinalIgnoreCase) ||
            versionJson?["type"]?.ToString() is "snapshot" or "pending") {
            return McInstanceState.Snapshot;
        }
        if (versionJson.ToString().Contains("optifine")) {
            version.HasOptiFine = true;
            version.OptiFineVersion = Regex.Match(versionJson.ToString(), "(?<=HD_U_)[^\"\":/]+")?.Value ?? "未知版本";
            return McInstanceState.OptiFine;
        }
        return McInstanceState.Original;
    }

    // 假设 GetMcFoolName 也移动到此静态类
    private static string GetMcFoolName(string name) {
        name = name.ToLowerInvariant();
        return name switch {
            var n when n.StartsWith("2.0") || n.StartsWith("2point0") =>
                $"2013 | 这个秘密计划了两年的更新将游戏推向了一个新高度！{(n.EndsWith("red") ? "（红色版本）" : n.EndsWith("blue") ? "（蓝色版本）" : n.EndsWith("purple") ? "（紫色版本）" : "")}",
            "15w14a" => "2015 | 作为一款全年龄向的游戏，我们需要和平，需要爱与拥抱。",
            "1.rv-pre1" => "2016 | 是时候将现代科技带入 Minecraft 了！",
            "3d shareware v1.34" => "2019 | 我们从地下室的废墟里找到了这个开发于 1994 年的杰作！",
            var n when n.StartsWith("20w14inf") || n == "20w14∞" => "2020 | 我们加入了 20 亿个新的维度，让无限的想象变成了现实！",
            "22w13oneblockatatime" => "2022 | 一次一个方块更新！迎接全新的挖掘、合成与骑乘玩法吧！",
            "23w13a_or_b" => "2023 | 研究表明：玩家喜欢作出选择——越多越好！",
            "24w14potato" => "2024 | 毒马铃薯一直都被大家忽视和低估，于是我们超级加强了它！",
            "25w14craftmine" => "2025 | 你可以合成任何东西——包括合成你的世界！",
            _ => ""
        };
    }

    /// <summary>
    /// Gets the default description for the instance.
    /// </summary>
    public static async Task<string> GetDefaultDescription(McInstance instance) {
        string info = "";
        McInstanceInfo version = await instance.GetVersionAsync();
        switch (instance.State) {
            case McInstanceState.Snapshot:
                if (version.McName.ContainsF("pre", true)) {
                    info = "预发布版 " + version.McName;
                } else if (version.McName.ContainsF("rc", true)) {
                    info = "发布候选 " + version.McName;
                } else if (version.McName.Contains("experimental") || version.McName == "pending") {
                    info = "实验性快照";
                } else {
                    info = "快照" + version.McName;
                }
                break;
            case McInstanceState.Old:
                info = "远古版本";
                break;
            case McInstanceState.Original:
            case McInstanceState.Forge:
            case McInstanceState.NeoForge:
            case McInstanceState.Fabric:
            case McInstanceState.LegacyFabric:
            case McInstanceState.Quilt:
            case McInstanceState.LabyMod:
            case McInstanceState.OptiFine:
            case McInstanceState.LiteLoader:
            case McInstanceState.Cleanroom:
                info = version.ToString();
                break;
            case McInstanceState.Fool:
                info = GetMcFoolName(version.McName);
                break;
            case McInstanceState.Error:
                return info; // Return existing error information
            default:
                info = "发生了未知错误，请向作者反馈此问题";
                break;
        }
        return info;
    }

    public static async Task<DateTime> TryGetReleaseTimeAsync(McInstance instance) {
        var jsonObject = await instance.GetVersionJsonAsync();
        if (jsonObject.TryGetPropertyValue("releaseTime", out var releaseTimeNode)) {
            if (releaseTimeNode != null && DateTime.TryParse(releaseTimeNode.GetValue<string>(), out var releaseTime)) {
                return releaseTime;
            }
        }
        return new DateTime(1970, 1, 1, 15, 0, 0);
    }

    public static string GetVersionFromJson(McInstance instance) {
        return "Unknown"; // 根据实际需求实现
    }
}
