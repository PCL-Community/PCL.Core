using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Instance;

/// <summary>
/// 表示一个 Minecraft 实例的版本信息和附加组件信息。
/// </summary>
public class McInstanceInfo {
    private Version? _mcVersion;

    private static readonly ImmutableDictionary<string, string> LoaderImageMap =
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
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 指示实例的 API 信息是否已加载。
    /// </summary>
    public bool IsApiLoaded { get; set; }

    /// <summary>
    /// 实例发布时间
    /// </summary>
    public DateTime ReleaseTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// 原版版本名，如 "1.12.2" 或 "16w01a"。
    /// </summary>
    public string McVersionStr { get; set; } = string.Empty;

    /// <summary>
    /// 可读的版本名
    /// </summary>
    public string FormattedVersion => McVersionStr == string.Empty ? "未知版本" : McFormatter.FormatVersion(McVersionStr);

    /// <summary>
    /// 是否为正常版本号格式的版本
    /// </summary>
    public bool IsNormalVersion => RegexPatterns.McNormalVersion.IsMatch(McVersionStr);

    /// <summary>
    /// MC 版本类型
    /// </summary>
    public McVersionType VersionType = McVersionType.Release;

    /// <summary>
    /// 标准的原版版本号。若为快照版或无效版本号，返回 0.0.0。
    /// </summary>
    // 私有支持字段，用于缓存解析结果

    public Version? McVersion {
        get {
            return _mcVersion ??= RefreshMcVersion();
        }
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

    #region Patchers

    public List<PatcherInfo> Patchers { get; } = [];

    public bool IsModded => HasAnyPatcher([
        "cleanroom", "liteloader", "forge", "neoforge", "fabric", "legacyfabric", "quilt"
    ]);

    public bool IsClient => HasAnyPatcher([
        "labymod", "optifine"
    ]);

    // 检查是否包含特定加载器
    public bool HasPatcher(string patcherId) {
        return Patchers.Any(p => p.Id!.Equals(patcherId, StringComparison.OrdinalIgnoreCase));
    }

    // 检查是否包含一组加载器中的任意一个
    public bool HasAnyPatcher(IEnumerable<string> patcherIds) {
        return patcherIds.Any(id => Patchers.Any(p => p.Id!.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }

    public PatcherInfo? GetPatcher(string patcherId) {
        return Patchers.FirstOrDefault(p => p.Id!.Equals(patcherId, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

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
            if (Patchers.Any(p => p.Id!.Equals(loader, StringComparison.OrdinalIgnoreCase))) {
                return Path.Combine(Basics.ImagePath, LoaderImageMap[loader]);
            }
        }

        // 正常版本
        return Path.Combine(Basics.ImagePath, "Blocks/Grass.png");
    }

    /*
    /// <summary>
    /// 生成用户友好的实例信息描述字符串。
    /// </summary>
    public override string ToString() {
        string result = string.Empty;
        if (HasForge) result += $", Forge{(ForgeVersion == "未知版本" ? "" : " " + ForgeVersion)}";
        if (HasNeoForge) result += $", NeoForge{(NeoForgeVersion == "未知版本" ? "" : " " + NeoForgeVersion)}";
        if (HasCleanroom) result += $", Cleanroom{(CleanroomVersion == "未知版本" ? "" : " " + CleanroomVersion)}";
        if (HasFabric) result += $", Fabric{(FabricVersion == "未知版本" ? "" : " " + FabricVersion)}";
        if (HasLegacyFabric) result += $", LegacyFabric{(LegacyFabricVersion == "未知版本" ? "" : " " + LegacyFabricVersion)}";
        if (HasQuilt) result += $", Quilt{(QuiltVersion == "未知版本" ? "" : " " + QuiltVersion)}";
        if (HasLabyMod) result += $", LabyMod{(LabyModVersion == "未知版本" ? "" : " " + LabyModVersion)}";
        if (HasOptiFine) result += $", OptiFine{(OptiFineVersion == "未知版本" ? "" : " " + OptiFineVersion)}";
        if (HasLiteLoader) result += ", LiteLoader";

        return result == string.Empty ? $"原版 {McVersion}" : $"{McVersion}{result}";
    }
    */
}

public enum McVersionType {
    Fool,
    Release,
    Snapshot,
    Old
}
