using System;
using System.Collections.Generic;
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
    private static readonly IReadOnlyList<string> DescStrings = new List<string> {
        "开启一段全新的冒险之旅！",
        "创造属于你的独特世界。",
        "探索无尽的可能性。",
        "随时随地，开始你的旅程。",
        "打造你的梦想之地。",
        "自由发挥，享受无限乐趣。",
        "一个属于你的 Minecraft 故事。",
        "发现新奇，创造精彩。",
        "轻松开启，畅玩无忧。",
        "你的冒险，从这里起航。",
        "构建、探索、尽情享受！",
        "适合每一位玩家的乐园。",
        "创造与冒险的完美结合。",
        "开启属于你的游戏篇章。",
        "探索未知，创造奇迹。",
        "属于你的 Minecraft 世界。",
        "简单上手，乐趣无穷。",
        "打造你的专属冒险舞台。",
        "从零开始，创造无限。",
        "你的故事，等待书写！"
    }.AsReadOnly();
    private static readonly Random Random = new Random();

    public static string GetRandomDescString() {
        return DescStrings[Random.Next(DescStrings.Count)];
    }
    
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
        var versionInfo = await instance.GetVersionInfoAsync();
        bool isRelease = versionInfo.IsReleaseVersion;
        LogWrapper.Info($"[Minecraft] 版本隔离初始化({instance.Name}): 全局设置({SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2)}), IsRelease {isRelease}, Modable {isModable}");

        var version = await instance.GetVersionInfoAsync();
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
        var version = await instance.GetVersionInfoAsync();
        return version.HasFabric || version.HasLegacyFabric || version.HasQuilt ||
               version.HasForge || version.HasLiteLoader || version.HasNeoForge ||
               version.HasCleanroom || instance.DisplayType == McInstanceCardType.API;
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
        McInstanceInfo versionInfo = await instance.GetVersionInfoAsync();
        if (instance.IsError) {
            return info;
        }
        if (versionInfo.IsFoolVersion) {
            return GetMcFoolName(versionInfo.McVersion);
        }
        return GetRandomDescString();
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
}
