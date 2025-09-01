using System;
using System.Text.RegularExpressions;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.McInstance;

/// <summary>
/// 表示一个 Minecraft 实例的版本信息和附加组件信息。
/// </summary>
public class McInstanceInfo {
    /// <summary>
    /// 指示实例的 API 信息是否已加载。
    /// </summary>
    public bool IsApiLoaded { get; set; }
    
    /// <summary>
    /// 实例发布时间
    /// </summary>
    public DateTime ReleaseTime { get; set; } = new DateTime(1970, 1, 1, 15, 0, 0);

    /// <summary>
    /// 是否可以判断版本号。
    /// </summary>
    public bool CanDetermineVersion { get; set; } = false;
    
    /// <summary>
    /// 原版版本名，如 "1.12.2" 或 "16w01a"。
    /// </summary>
    public string McVersion { get; set; } = string.Empty;
    
    /// <summary>
    /// 可读的版本名
    /// </summary>
    public string FormattedMcVersion => McVersion == string.Empty ? "未知版本" : McFormatter.FormatVersion(McVersion);

    /// <summary>
    /// 是否为正式版（Release），即版本号形如 "1.12.2" 而非 "16w01a" 或 "1.12.2-pre1"。
    /// </summary>
    public bool IsReleaseVersion => RegexPatterns.McReleaseVersion.IsMatch(McVersion);

    /// <summary>
    /// 是否为愚人节版（Fool）。
    /// </summary>
    public bool IsFoolVersion => ReleaseTime.Month == 4 && ReleaseTime.Day == 1;
    
    /// <summary>
    /// 是否为快照版（Snapshot）。
    /// </summary>
    public bool IsSnapshotVersion => !IsReleaseVersion && IsFoolVersion;

    /// <summary>
    /// 是否为远古版（Old）。
    /// </summary>
    public bool IsOldVersion => ReleaseTime.Year > 2000 && ReleaseTime <= new DateTime(2011, 11, 16);

    /// <summary>
    /// 原版主版本号，如 12（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int McCodeMain { get; set; } = -1;

    /// <summary>
    /// 原版次版本号，如 2（对于 1.12.2），不可用为 -1。
    /// </summary>
    public int McCodeSub { get; set; } = -1;

    /// <summary>
    /// 标准的原版版本号。若为快照版或无效版本号，返回 0.0.0。
    /// </summary>
    public Version McInstance => IsReleaseVersion ? new Version(1, McCodeMain, McCodeSub) : new Version(0, 0, 0);

    // OptiFine
    /// <summary>
    /// 是否通过 JSON 安装了 OptiFine。
    /// </summary>
    public bool HasOptiFine { get; set; }

    /// <summary>
    /// OptiFine 版本号，如 "C8" 或 "C9_pre10"。
    /// </summary>
    public string OptiFineVersion { get; set; } = string.Empty;

    // Forge
    /// <summary>
    /// 是否安装了 Forge。
    /// </summary>
    public bool HasForge { get; set; }

    /// <summary>
    /// Forge 版本号，如 "31.1.2" 或 "14.23.5.2847"。
    /// </summary>
    public string ForgeVersion { get; set; } = string.Empty;

    // NeoForge
    /// <summary>
    /// 是否安装了 NeoForge。
    /// </summary>
    public bool HasNeoForge { get; set; }

    /// <summary>
    /// NeoForge 版本号，如 "21.0.2-beta" 或 "47.1.79"。
    /// </summary>
    public string NeoForgeVersion { get; set; } = string.Empty;

    // Cleanroom
    /// <summary>
    /// 是否安装了 Cleanroom。
    /// </summary>
    public bool HasCleanroom { get; set; }

    /// <summary>
    /// Cleanroom 版本号，如 "0.2.4-alpha"。
    /// </summary>
    public string CleanroomVersion { get; set; } = string.Empty;

    // Fabric
    /// <summary>
    /// 是否安装了 Fabric。
    /// </summary>
    public bool HasFabric { get; set; }

    /// <summary>
    /// Fabric 版本号，如 "0.7.2.175"。
    /// </summary>
    public string FabricVersion { get; set; } = string.Empty;

    // LegacyFabric
    /// <summary>
    /// 是否安装了 LegacyFabric。
    /// </summary>
    public bool HasLegacyFabric { get; set; }

    /// <summary>
    /// LegacyFabric 版本号，如 "0.7.2.175"。
    /// </summary>
    public string LegacyFabricVersion { get; set; } = string.Empty;

    // Quilt
    /// <summary>
    /// 是否安装了 Quilt。
    /// </summary>
    public bool HasQuilt { get; set; }

    /// <summary>
    /// Quilt 版本号，如 "0.26.1-beta.1" 或 "0.26.0"。
    /// </summary>
    public string QuiltVersion { get; set; } = string.Empty;

    // LabyMod
    /// <summary>
    /// 是否安装了 LabyMod。
    /// </summary>
    public bool HasLabyMod { get; set; }

    /// <summary>
    /// LabyMod 版本号，如 "4.2.59"。
    /// </summary>
    public string LabyModVersion { get; set; } = string.Empty;

    // LiteLoader
    /// <summary>
    /// 是否安装了 LiteLoader。
    /// </summary>
    public bool HasLiteLoader { get; set; }

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
}
