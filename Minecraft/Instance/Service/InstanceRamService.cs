using System;
using System.IO;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Utils.OS;

namespace PCL.Core.Minecraft.Instance.Service;

public static class InstanceRamService {
    /// <summary>
    /// 获取当前 Minecraft 实例的 RAM 设置值，单位为 GB。
    /// </summary>
    /// <param name="instance">Minecraft 实例配置。</param>
    /// <param name="is32BitJava">是否使用 32 位 Java，可为 null 表示自动检测。</param>
    /// <returns>分配的 RAM 大小（GB）。</returns>
    /// <remarks>
    /// 如果设置为跟随全局配置，则调用 PageSetupLaunch.GetRam。
    /// 否则根据实例配置（自动或手动）计算 RAM 大小。
    /// 32 位 Java 环境下 RAM 限制为最大 1GB。
    /// </remarks>
    public static double GetRam(IMcInstance instance, bool? is32BitJava = null) {
        // 跟随全局设置
        if (Config.Instance.MemorySolution[instance.Path] == 2) {
            return PageSetupLaunch.GetRam(instance, true, is32BitJava);
        }

        // 注意：修改以下代码时，需同步更新 PageSetupLaunch

        double ramGive;
        if (Setup.Get("VersionRamType", instance: instance) == 0) {
            // 自动配置
            double ramAvailable = Math.Round(KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024.0 / 1024 / 1024 * 10) / 10;

            // 确定内存需求
            if (instance is not null && !instance.IsLoaded) {
                instance.Load();
            }

            double ramMinimum, ramTarget1, ramTarget2, ramTarget3;
            if (instance is not null && instance.Modable) {
                // 可安装 Mod 的实例
                DirectoryInfo modDir = new(instance.PathIndie + "mods\\");
                int modCount = modDir.Exists ? modDir.GetFiles().Length : 0;
                ramMinimum = 0.5 + modCount / 150.0;
                ramTarget1 = 1.5 + modCount / 90.0;
                ramTarget2 = 2.7 + modCount / 50.0;
                ramTarget3 = 4.5 + modCount / 25.0;
            } else if (instance is not null && instance.Version.HasOptiFine) {
                // OptiFine 实例
                ramMinimum = 0.5;
                ramTarget1 = 1.5;
                ramTarget2 = 3.0;
                ramTarget3 = 5.0;
            } else {
                // 普通实例
                ramMinimum = 0.5;
                ramTarget1 = 1.5;
                ramTarget2 = 2.5;
                ramTarget3 = 4.0;
            }

            // 分阶段分配内存
            ramGive = 0;
            double[] stages = [
                ramTarget1, // 阶段一：0 ~ T1，100%
                ramTarget2 - ramTarget1, // 阶段二：T1 ~ T2，70%
                ramTarget3 - ramTarget2, // 阶段三：T2 ~ T3，40%
                ramTarget3 // 阶段四：T3 ~ T3*2，15%
            ];
            double[] percentages = [1.0, 0.7, 0.4, 0.15];

            foreach (var (delta, percentage) in stages.Zip(percentages)) {
                if (ramAvailable < 0.1) {
                    break;
                }
                double allocated = Math.Min(ramAvailable * percentage, delta);
                ramGive += allocated;
                ramAvailable -= delta / percentage;
            }

            // 确保不低于最低值
            ramGive = Math.Round(Math.Max(ramGive, ramMinimum), 1);
        } else {
            // 手动配置
            int value = Setup.Get("VersionRamCustom", instance: instance) ?? 0;
            ramGive = value switch {
                <= 12 => value * 0.1 + 0.3,
                <= 25 => (value - 12) * 0.5 + 1.5,
                <= 33 => (value - 25) * 1.0 + 8.0,
                _ => (value - 33) * 2.0 + 16.0
            };
        }

        // 32 位 Java 限制为最大 1GB
        bool is32Bit = is32BitJava ?? !IsGameSet64BitJava(PageInstanceLeft.Instance);
        if (is32Bit) {
            ramGive = Math.Min(1.0, ramGive);
        }

        return ramGive;
    }

    /// <summary>
    /// 获取当前 Minecraft 实例的 RAM 设置值，单位为 GB。
    /// </summary>
    /// <param name="instance">Minecraft 实例配置，可能为 null。</param>
    /// <param name="useVersionJavaSetup">是否使用实例特定的 Java 配置。</param>
    /// <param name="is32BitJava">是否使用 32 位 Java，可为 null 表示自动检测。</param>
    /// <returns>分配的 RAM 大小（GB）。</returns>
    /// <remarks>
    /// 根据全局或实例配置（自动或手动）计算 RAM 大小。
    /// 32 位 Java 环境下 RAM 限制为最大 1GB。
    /// 修改此方法时，需同步更新 PageInstanceSetup。
    /// </remarks>
    public static double GetRam(McInstance? instance, bool useVersionJavaSetup, bool? is32BitJava = null) {
        // 注意：修改以下代码时，需同步更新 PageInstanceSetup

        double ramGive;
        if (Setup.Get("LaunchRamType") == 0) { } else {
            // 手动配置
            int value = Setup.Get("LaunchRamCustom") ?? 0;
            ramGive = value switch {
                <= 12 => value * 0.1 + 0.3,
                <= 25 => (value - 12) * 0.5 + 1.5,
                <= 33 => (value - 25) * 1.0 + 8.0,
                _ => (value - 33) * 2.0 + 16.0
            };
        }

        // 32 位 Java 限制为最大 1GB
        bool is32Bit = is32BitJava ?? !IsGameSet64BitJava(useVersionJavaSetup ? instance : null);
        if (is32Bit) {
            ramGive = Math.Min(1.0, ramGive);
        }

        return ramGive;
    }

    public static double getRam2() {
        // 自动配置
        double ramAvailable = Math.Round(KernelInterop.GetAvailablePhysicalMemoryBytes() / 1024.0 / 1024 / 1024 * 10) / 10;

        // 确定内存需求
        if (instance is not null && !instance.IsLoaded) {
            instance.Load();
        }

        double ramMinimum, ramTarget1, ramTarget2, ramTarget3;
        if (instance is not null && instance.Modable) {
            // 可安装 Mod 的实例
            DirectoryInfo modDir = new(instance.PathIndie + "mods\\");
            int modCount = modDir.Exists ? modDir.GetFiles().Length : 0;
            ramMinimum = 0.5 + modCount / 150.0;
            ramTarget1 = 1.5 + modCount / 90.0;
            ramTarget2 = 2.7 + modCount / 50.0;
            ramTarget3 = 4.5 + modCount / 25.0;
        } else if (instance is not null && instance.Version.HasOptiFine) {
            // OptiFine 实例
            ramMinimum = 0.5;
            ramTarget1 = 1.5;
            ramTarget2 = 3.0;
            ramTarget3 = 5.0;
        } else {
            // 普通实例
            ramMinimum = 0.5;
            ramTarget1 = 1.5;
            ramTarget2 = 2.5;
            ramTarget3 = 4.0;
        }

        // 分阶段分配内存
        ramGive = 0;
        double[] stages = [
            ramTarget1, // 阶段一：0 ~ T1，100%
            ramTarget2 - ramTarget1, // 阶段二：T1 ~ T2，70%
            ramTarget3 - ramTarget2, // 阶段三：T2 ~ T3，40%
            ramTarget3 // 阶段四：T3 ~ T3*2，15%
        ];
        double[] percentages = [1.0, 0.7, 0.4, 0.15];

        foreach (var (delta, percentage) in stages.Zip(percentages)) {
            if (ramAvailable < 0.1) {
                break;
            }
            double allocated = Math.Min(ramAvailable * percentage, delta);
            ramGive += allocated;
            ramAvailable -= delta / percentage;
        }

        // 确保不低于最低值
        ramGive = Math.Round(Math.Max(ramGive, ramMinimum), 1);
    }
}
