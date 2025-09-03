using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using PCL.Core.App;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.McInstance;

/// <summary>
/// 表示一个 Minecraft 实例的版本信息和附加组件信息。
/// </summary>
public class McInstanceInfo {
    private static readonly ImmutableDictionary<string, string> LoaderImageMap = 
        new Dictionary<string, string>
        {
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
    public string McVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// 可读的版本名
    /// </summary>
    public string FormattedVersion => McVersion == string.Empty ? "未知版本" : McFormatter.FormatVersion(McVersion);

    /// <summary>
    /// 是否为正常版本号格式的版本
    /// </summary>
    public bool IsNormalVersion => RegexPatterns.McNormalVersion.IsMatch(McVersion);
    
    /// <summary>
    /// MC 版本类型
    /// </summary>
    public McVersionType VersionType = McVersionType.Release;

    /*
    /// <summary>
    /// 原版主版本号，如 12（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int McVersionMinor = IsNormalVersion

    /// <summary>
    /// 原版次版本号，如 2（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int McVersionPatch

    /// <summary>
    /// 标准的原版版本号。若为快照版或无效版本号，返回 0.0.0。
    /// </summary>
    public Version McInstance => VersionType == McVersionType.Release ? new Version(1, McCodeMain, McCodeSub) : new Version(0, 0, 0);
    */
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
            if (Patchers.Any(p => p.Id!.Equals(loader, StringComparison.OrdinalIgnoreCase)))
            {
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

    private int _sortCode = -2;

    /// <summary>
    /// 用于排序比较的编号。
    /// </summary>
    public int SortCode {
        get {
            if (_sortCode == -2) {
                try {
                    _sortCode = CalculateSortCode();
                } catch (Exception ex) {
                    _sortCode = -1;
                    Console.WriteLine($"获取 API 版本信息失败：{ToString()} - {ex.Message}");
                }
            }
            return _sortCode;
        }
        set => _sortCode = value;
    }

    private int CalculateSortCode() {
        if (HasLegacyFabric || HasFabric) {
            string version = HasLegacyFabric ? LegacyFabricVersion : FabricVersion;
            if (version == "未知版本") return 0;
            var subVersions = version.Split('.');
            if (subVersions.Length >= 3)
                return int.Parse(subVersions[0]) * 10000 + int.Parse(subVersions[1]) * 100 + int.Parse(subVersions[2]);
            throw new Exception($"无效的 {(HasLegacyFabric ? "LegacyFabric" : "Fabric")} 版本：{version}");
        }

        if (HasQuilt) {
            if (QuiltVersion == "未知版本") return 0;
            bool isBeta = QuiltVersion.Contains("-beta", StringComparison.OrdinalIgnoreCase);
            var subVersions = QuiltVersion.Replace("-beta", "", StringComparison.OrdinalIgnoreCase).Split('.');
            if (subVersions.Length >= 3)
                return int.Parse(subVersions[0]) * 10000 + int.Parse(subVersions[1]) * 100 + int.Parse(subVersions[2]) + (isBeta ? 1 : 0);
            throw new Exception($"无效的 Quilt 版本：{QuiltVersion}");
        }

        if (HasCleanroom) {
            if (CleanroomVersion == "未知版本") return 0;
            bool isAlpha = CleanroomVersion.Contains("-alpha", StringComparison.OrdinalIgnoreCase);
            var subVersions = CleanroomVersion.Replace("-alpha", "", StringComparison.OrdinalIgnoreCase).Split('.');
            if (subVersions.Length >= 3)
                return int.Parse(subVersions[0]) * 10000 + int.Parse(subVersions[1]) * 100 + int.Parse(subVersions[2]) + (isAlpha ? 1 : 0);
            throw new Exception($"无效的 Cleanroom 版本：{CleanroomVersion}");
        }

        if (HasForge || HasNeoForge) {
            string version = HasForge ? ForgeVersion : NeoForgeVersion;
            if (version == "未知版本") return 0;
            var subVersions = version.Split('.');
            if (subVersions.Length == 4)
                return int.Parse(subVersions[0]) * 1000000 + int.Parse(subVersions[1]) * 10000 + int.Parse(subVersions[3]);
            if (subVersions.Length == 3)
                return int.Parse(subVersions[0]) * 1000000 + int.Parse(subVersions[1]) * 10000 + int.Parse(subVersions[2]);
            throw new Exception($"无效的 {(HasForge ? "Forge" : "NeoForge")} 版本：{version}");
        }

        if (HasLabyMod) {
            if (LabyModVersion == "未知版本") return 0;
            var subVersions = LabyModVersion.Split('.');
            if (subVersions.Length == 4)
                return int.Parse(subVersions[0]) * 1000000 + int.Parse(subVersions[1]) * 10000 + int.Parse(subVersions[3]);
            if (subVersions.Length == 3)
                return int.Parse(subVersions[0]) * 1000000 + int.Parse(subVersions[1]) * 10000 + int.Parse(subVersions[2]);
            throw new Exception($"无效的 LabyMod 版本：{LabyModVersion}");
        }

        if (HasOptiFine) {
            if (OptiFineVersion == "未知版本") return 0;
            var code = (McCodeSub >= 0 ? McCodeSub : 0) * 1000000;
            var letter = OptiFineVersion[0].ToString().ToUpper();
            code += (letter[0] - 'A' + 1) * 10000;
            var numberMatch = Regex.Match(OptiFineVersion[1..], @"\d+").Value;
            code += int.Parse(numberMatch) * 100;

            if (OptiFineVersion.Contains("pre", StringComparison.OrdinalIgnoreCase))
                code += 50 + (int.TryParse(Regex.Match(OptiFineVersion, @"(?<=pre)\d+").Value, out int preNum) ? preNum : 1);
            else if (OptiFineVersion.Contains("beta", StringComparison.OrdinalIgnoreCase))
                code += int.TryParse(Regex.Match(OptiFineVersion, @"(?<=beta)\d+").Value, out int betaNum) ? betaNum : 1;
            else
                code += 99;

            return code;
        }

        return -1;
    }
    */
}

public enum McVersionType {
    Fool,
    Release,
    Snapshot,
    Old
}
