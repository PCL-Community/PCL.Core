namespace PCL.Core.Minecraft.McLaunch.Services.Java;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Java版本选择器
/// </summary>
public static class JavaSelector {
    /// <summary>
    /// 根据版本要求选择最佳Java
    /// </summary>
    public static async Task<Result<Java>> SelectBestJavaAsync(Version minVersion, Version maxVersion, McInstance instance) {
        try {
            return await Task.Run(() => SelectBestJava(minVersion, maxVersion, instance));
        } catch (Exception ex) {
            return Result<Java>.Failed($"选择Java失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 分析实例的Java版本要求
    /// </summary>
    public static JavaRequirement AnalyzeRequirements(McInstance.McInstance instance) {
        var minVer = new Version(0, 0, 0, 0);
        var maxVer = new Version(999, 999, 999, 999);

        // MC 大版本检测
        if ((!instance.Version.IsStandardVersion && instance.ReleaseTime >= new DateTime(2024, 4, 2)) ||
            (instance.Version.IsStandardVersion && instance.Version.McInstance >= new Version(1, 20, 5))) {
            // 1.20.5+ (24w14a+)：至少 Java 21
            minVer = new Version(21, 0, 0, 0);
        } else if ((!instance.Version.IsStandardVersion && instance.ReleaseTime >= new DateTime(2021, 11, 16)) ||
                   (instance.Version.IsStandardVersion && instance.Version.McInstance >= new Version(1, 18))) {
            // 1.18 pre2+：至少 Java 17
            minVer = new Version(17, 0, 0, 0);
        } else if ((!instance.Version.IsStandardVersion && instance.ReleaseTime >= new DateTime(2021, 5, 11)) ||
                   (instance.Version.IsStandardVersion && instance.Version.McInstance >= new Version(1, 17))) {
            // 1.17+ (21w19a+)：至少 Java 16
            minVer = new Version(16, 0, 0, 0);
        } else if (instance.ReleaseTime.Year >= 2017) {
            // 1.12+：至少 Java 8
            minVer = new Version(1, 8, 0, 0);
        } else if (instance.ReleaseTime <= new DateTime(2013, 5, 1) && instance.ReleaseTime.Year >= 2001) {
            // 1.5.2-：最高 Java 12
            maxVer = new Version(12, 999, 999, 999);
        }

        // JSON中推荐的Java版本
        if (instance.JsonVersion?["java_version"] != null) {
            var recommendedJava = instance.JsonVersion["java_version"].ToObject<int>();
            if (recommendedJava >= 22)
                minVer = new Version(recommendedJava, 0, 0, 0);
        }

        // OptiFine检测
        AnalyzeOptiFineRequirements(instance, ref minVer, ref maxVer);

        // Forge检测
        AnalyzeForgeRequirements(instance, ref minVer, ref maxVer);

        // 其他Mod检测
        AnalyzeOtherModRequirements(instance, ref minVer, ref maxVer);

        return new JavaRequirement {
            MinVersion = minVer,
            MaxVersion = maxVer,
            Instance = instance
        };
    }

    private static Result<Java> SelectBestJava(Version minVersion, Version maxVersion, McInstance instance) {
        // 这里应该调用原来的JavaSelect方法
        // 简化实现，返回占位符
        var selectedJava = JavaSelect("$$", minVersion, maxVersion, instance);

        if (selectedJava != null)
            return Result<Java>.Success(selectedJava);

        return Result<Java>.Failed($"未找到符合要求的Java版本 (要求: {minVersion} - {maxVersion})");
    }

    private static void AnalyzeOptiFineRequirements(McInstance instance, ref Version minVer, ref Version maxVer) {
        if (!instance.Version.HasOptiFine || !instance.Version.IsStandardVersion)
            return;

        if (instance.Version.McInstance < new Version(1, 7) || instance.Version.McCodeMain == 12) {
            // <1.7 / 1.12：最高 Java 8
            maxVer = new Version(8, 999, 999, 999);
        } else if (instance.Version.McInstance >= new Version(1, 8) && instance.Version.McInstance < new Version(1, 12)) {
            // 1.8 - 1.11：必须恰好 Java 8
            minVer = new Version(1, 8, 0, 0);
            maxVer = new Version(8, 999, 999, 999);
        }
    }

    private static void AnalyzeForgeRequirements(McInstance instance, ref Version minVer, ref Version maxVer) {
        if (!instance.Version.HasForge)
            return;

        if (instance.Version.McInstance >= new Version(1, 6, 1) && instance.Version.McInstance <= new Version(1, 7, 2)) {
            // 1.6.1 - 1.7.2：必须 Java 7
            minVer = Version.Max(new Version(1, 7, 0, 0), minVer);
            maxVer = Version.Min(new Version(1, 7, 999, 999), maxVer);
        } else if (instance.Version.McCodeMain <= 12 || !instance.Version.IsStandardVersion) {
            // <=1.12：Java 8
            maxVer = new Version(8, 999, 999, 999);
        }
        // 其他Forge版本要求...
    }

    private static void AnalyzeOtherModRequirements(McInstance instance, ref Version minVer, ref Version maxVer) {
        // LiteLoader检测
        if (instance.Version.HasLiteLoader && instance.Version.IsStandardVersion) {
            maxVer = Version.Min(new Version(8, 999, 999, 999), maxVer);
        }

        // Cleanroom检测
        if (instance.Version.HasCleanroom) {
            minVer = Version.Max(new Version(21, 0, 0, 0), minVer);
        }

        // Fabric检测
        if (instance.Version.HasFabric && instance.Version.IsStandardVersion) {
            if (instance.Version.McCodeMain >= 15 && instance.Version.McCodeMain <= 16) {
                minVer = Version.Max(new Version(1, 8, 0, 0), minVer);
            } else if (instance.Version.McCodeMain >= 18) {
                minVer = Version.Max(new Version(17, 0, 0, 0), minVer);
            }
        }

        // LabyMod检测
        if (instance.Version.HasLabyMod) {
            minVer = Version.Max(new Version(21, 0, 0, 0), minVer);
            maxVer = new Version(999, 999, 999, 999);
        }
    }

    // 版本比较扩展方法
    private static Version Max(Version v1, Version v2) {
        return v1 > v2 ? v1 : v2;
    }

    private static Version Min(Version v1, Version v2) {
        return v1 < v2 ? v1 : v2;
    }

    // 这些是从原VB代码中需要引用的方法和属性的占位符
    private static Java JavaSelect(string placeholder, Version minVer, Version maxVer, McInstance instance) {
        throw new NotImplementedException("需要从原代码引用JavaSelect方法");
    }
}

/// <summary>
/// Java版本要求
/// </summary>
public class JavaRequirement {
    public Version MinVersion { get; set; }
    public Version MaxVersion { get; set; }
    public McInstance Instance { get; set; }

    public override string ToString() {
        return $"Java要求: {MinVersion} - {MaxVersion} (实例: {Instance?.Name})";
    }
}
