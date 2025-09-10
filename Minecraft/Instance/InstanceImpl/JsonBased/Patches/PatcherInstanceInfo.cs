using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance.Handler;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Instance.InstanceImpl.JsonBased.Patches;

/// <summary>
/// 表示一个 Minecraft 实例的版本信息和附加组件信息。
/// </summary>
public class PatcherInstanceInfo : IMcInstanceInfo {
    private Version? _mcVersion;

    private static readonly FrozenDictionary<string, string> PatchersImageMap =
        new Dictionary<string, string> {
            { "neoforge", "Blocks/NeoForge.png" },
            { "fabric", "Blocks/Fabric.png" },
            { "legacyfabric", "Blocks/Fabric.png" },
            { "forge", "Blocks/Forge.png" },
            { "liteloader", "Blocks/Egg.png" },
            { "quilt", "Blocks/Quilt.png" },
            { "cleanroom", "Blocks/Cleanroom.png" },
            { "labymod", "Blocks/LabyMod.png" },
            { "optifine", "Blocks/OptiFine.png" }
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    public DateTime ReleaseTime { get; set; } = DateTime.MinValue;

    public string McVersionStr { get; } = Patchers.Find(p => p.Id == "game")!.Version!;

    public string FormattedVersion => InstanceInfoHandler.GetFormattedVersion(McVersionStr);

    public bool IsNormalVersion => InstanceInfoHandler.IsNormalVersion(McVersionStr);
    
    public McVersionType VersionType { get; set; } = McVersionType.Release;
    
    public Version? McVersion {
        get => _mcVersion ??= RefreshMcVersion();
        set => _mcVersion = value;
    }

    private Version? RefreshMcVersion() {
        return IsNormalVersion ? Version.Parse(McVersionStr) : null;
    }
    
    /// <summary>
    /// 原版主版本号，如 12（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int? McVersionMinor => McVersion?.Minor;

    /// <summary>
    /// 原版次版本号，如 2（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int? McVersionBuild => McVersion?.Build;

    private static List<PatcherInfo> Patchers { get; } = [];

    public bool IsModded => HasAnyPatcher([
        "cleanroom", "liteloader", "forge", "neoforge", "fabric", "legacyfabric", "quilt"
    ]);

    public bool IsClient => HasAnyPatcher([
        "labymod", "optifine"
    ]);

    // 检查是否包含特定加载器
    public bool HasPatcher(string patcherId) {
        return Patchers.Any(p => p.Id.Equals(patcherId, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetPatcherVersion(string id) {
        return Patchers.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Version ?? string.Empty;
    }

    // 检查是否包含一组加载器中的任意一个
    public bool HasAnyPatcher(IEnumerable<string> patcherIds) {
        return patcherIds.Any(id => Patchers.Any(p => p.Id!.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }

    public PatcherInfo? GetPatcher(string patcherId) {
        return Patchers.FirstOrDefault(p => p.Id!.Equals(patcherId, StringComparison.OrdinalIgnoreCase));
    }

    public string GetLogo() {
        switch (VersionType) {
            case McVersionType.Fool:
                return Path.Combine(Basics.ImagePath, "Blocks/GoldBlock.png");
            case McVersionType.Old:
                return Path.Combine(Basics.ImagePath, "Blocks/CobbleStone.png");
            case McVersionType.Snapshot:
                return Path.Combine(Basics.ImagePath, "Blocks/CommandBlock.png");
            case McVersionType.Release:
                break;
            default:
                return Path.Combine(Basics.ImagePath, "Blocks/RedstoneBlock.png");
        }

        // 其次判断加载器等
        foreach (var loader in new[] { "neoforge", "fabric", "legacyFabric", "forge", "liteloader", "quilt", "cleanroom", "labymod", "optifine" }) {
            if (Patchers.Any(p => p.Id.Equals(loader, StringComparison.OrdinalIgnoreCase))) {
                return Path.Combine(Basics.ImagePath, PatchersImageMap[loader]);
            }
        }

        // 正常版本
        return Path.Combine(Basics.ImagePath, "Blocks/Grass.png");
    }
}

