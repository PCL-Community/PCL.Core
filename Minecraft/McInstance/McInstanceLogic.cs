using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Minecraft.McFolder;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.McInstance;

public static class McInstanceLogic {
    private static readonly ImmutableList<string> DescStrings = ImmutableList.Create<string> (
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
    );
    
    /// <summary>
    /// Gets the default description for the instance.
    /// </summary>
    public static async Task<string> GetDefaultDescription(McInstance instance) {
        var versionInfo = await instance.GetVersionInfoAsync();
        if (instance.DisplayType == McInstanceCardType.Error) {
            return "";
        }
        return versionInfo!.VersionType == McVersionType.Fool ? McInstanceUtils.GetMcFoolVersionDesc(versionInfo.McVersion) : RandomUtils.PickRandom(DescStrings);
    }
    
    /// <summary>
    /// 获取实例的隔离路径，根据全局设置和实例特性决定是否使用独立文件夹。
    /// </summary>
    /// <param name="instance">Minecraft 实例</param>
    /// <returns>隔离后的路径，以“\”结尾</returns>
    public static async Task<string?> GetIsolatedPathAsync(McInstance instance) {
        if (instance.DisplayType == McInstanceCardType.Error) {
            return null;
        }
        
        if (SetupService.IsUnset(SetupEntries.Instance.IndieV2, instance.Path)) {
            var shouldBeIndie = await ShouldBeIndieAsync(instance);
            SetupService.SetBool(SetupEntries.Instance.IndieV2, shouldBeIndie, instance.Path);
        }
        return SetupService.GetBool(SetupEntries.Instance.IndieV2) ? instance.Path : McFolderManager.PathMcFolder;
    }

    private static async Task<bool> ShouldBeIndieAsync(McInstance instance) {
        var versionInfo = await instance.GetVersionInfoAsync();
        
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

        var isModded = versionInfo!.IsModded;
        var isRelease = versionInfo.VersionType == McVersionType.Release;
        LogWrapper.Info($"[Minecraft] 版本隔离初始化({instance.Name}): 全局设置({SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2)})");
        
        return SetupService.GetInt32(SetupEntries.Launch.IndieSolutionV2) switch {
            0 => false,
            1 => versionInfo.HasPatcher("labymod") || isModded,
            2 => !isRelease,
            3 => versionInfo.HasPatcher("labymod") || isModded || !isRelease,
            _ => true
        };
    }
    
    public static async Task<string> DetermineLogo(McInstance instance) {
        var logo = SetupService.GetString(SetupEntries.Instance.LogoPath, instance.Path);
        var versionInfo = await instance.GetVersionInfoAsync();
        if (string.IsNullOrEmpty(logo) || !SetupService.GetBool(SetupEntries.Instance.IsLogoCustom, instance.Path)) {
            if (instance.DisplayType == McInstanceCardType.Error) {
                return Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
            }
            return versionInfo.GetLogo();
        }
        return logo;
    }
}
