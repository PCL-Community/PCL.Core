using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Folder;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Minecraft.Instance.Service;
using PCL.Core.Minecraft.Launch.Utils;
using PCL.Core.UI;

namespace PCL.Core.Minecraft.Launch.Services;

/// <summary>
/// Java版本选择服务
/// 负责根据Minecraft版本要求选择合适的Java版本
/// </summary>
public class JavaSelectService(IMcInstance instance) {
    private const string USE_GLOBAL_SETTING = "使用全局设置";

    /// <summary>
    /// 根据当前实例版本要求选择最佳Java
    /// </summary>
    /// <returns>选择的Java信息，如果选择失败则抛出异常</returns>
    public async Task<JavaInfo> SelectBestJavaAsync() {
        var (minVersion, maxVersion) = GetJavaVersionRequirements();

        var selectedJava = await SelectJavaByPriorityAsync(minVersion, maxVersion);
        if (selectedJava != null) {
            McLaunchUtils.Log($"成功选择Java：{selectedJava}");
            return selectedJava;
        }

        McLaunchUtils.Log("未找到合适的Java，尝试自动下载");
        var downloadedJava = await HandleJavaDownloadAsync(minVersion, maxVersion);

        return downloadedJava ?? throw new InvalidOperationException("无法获取合适的Java版本");
    }

    /// <summary>
    /// 按优先级选择Java：实例指定 > 全局指定 > 自动搜索
    /// </summary>
    private async Task<JavaInfo?> SelectJavaByPriorityAsync(Version minVersion, Version maxVersion) {
        LogWrapper.Info($"开始选择Java - 最低版本：{minVersion}，最高版本：{maxVersion}，实例：{instance.Name}");

        // 1. 优先使用实例指定的Java
        var instanceJava = GetInstanceSpecifiedJava();
        if (instanceJava != null) {
            if (!IsJavaVersionSuitable(instanceJava.Version, minVersion, maxVersion)) {
                HintWrapper.Show("当前实例指定的Java版本可能不合适，可能导致游戏崩溃");
            }
            LogWrapper.Info($"使用实例指定Java：{instanceJava}");
            return instanceJava;
        }

        // 2. 使用全局指定的Java
        var globalJava = GetGlobalSpecifiedJava();
        if (globalJava != null) {
            LogWrapper.Info($"使用全局指定Java：{globalJava}");
            return globalJava;
        }

        // 3. 自动搜索合适的Java
        return await SearchSuitableJavaAsync(minVersion, maxVersion);
    }

    /// <summary>
    /// 处理Java自动下载逻辑
    /// </summary>
    private async Task<JavaInfo?> HandleJavaDownloadAsync(Version minVersion, Version maxVersion) {
        var javaSpec = DetermineRequiredJavaSpec(minVersion, maxVersion);

        if (!ConfirmJavaDownload(javaSpec)) {
            return null;
        }

        // TODO: 实现Java下载逻辑
        // return await DownloadJava(javaSpec.Code);

        // 暂时返回null，等待下载逻辑实现
        return null;
    }

    /// <summary>
    /// 确定所需的Java规格
    /// </summary>
    private JavaSpecification DetermineRequiredJavaSpec(Version minVersion, Version maxVersion) {
        // Java 22+
        if (minVersion >= new Version(22, 0)) {
            return new JavaSpecification($"Java {minVersion.Minor}", minVersion.Minor.ToString(), false);
        }

        // Java 21
        if (minVersion >= new Version(21, 0)) {
            return new JavaSpecification("Java 21", "21", false);
        }

        // Java 17 (用于1.9+)
        if (minVersion >= new Version(1, 9)) {
            return new JavaSpecification("Java 17", "17", false);
        }

        // Java 7 (用于1.8以下)
        if (maxVersion < new Version(1, 8)) {
            var hasForge = instance.InstanceInfo.HasPatch("forge");
            return new JavaSpecification("Java 7", "7", hasForge);
        }

        // Java 8的各种情况
        return DetermineJava8Specification(minVersion, maxVersion);
    }

    /// <summary>
    /// 确定Java 8的具体规格
    /// </summary>
    private static JavaSpecification DetermineJava8Specification(Version minVersion, Version maxVersion) {
        if (minVersion > new Version(1, 8, 0, 140) && maxVersion < new Version(1, 8, 0, 321)) {
            return new JavaSpecification("Java 8.0.141 ~ 8.0.320", "8u141", true);
        }

        if (minVersion > new Version(1, 8, 0, 140)) {
            return new JavaSpecification("Java 8.0.141 或更高版本的 Java 8", "8u141", true);
        }

        if (maxVersion < new Version(1, 8, 0, 321)) {
            return new JavaSpecification("Java 8.0.320 或更低版本的 Java 8", "8", false);
        }

        return new JavaSpecification("Java 8", "8", false);
    }

    /// <summary>
    /// 确认是否下载Java
    /// </summary>
    private bool ConfirmJavaDownload(JavaSpecification javaSpec) {
        if (javaSpec.RequiresManualDownload) {
            ShowManualDownloadMessage(javaSpec.DisplayName);
            return false;
        }

        if (javaSpec.DisplayName == "Java 7" && instance.InstanceInfo.HasPatch("forge")) {
            MsgBoxWrapper.Show(
                "你需要先安装 LegacyJavaFixer Mod，或自行安装 Java 7，然后才能启动该版本。",
                "未找到 Java");
            return false;
        }

        return ShowAutoDownloadConfirmation(javaSpec.DisplayName);
    }

    /// <summary>
    /// 显示手动下载提示
    /// </summary>
    private static void ShowManualDownloadMessage(string javaName) {
        MsgBoxWrapper.Show(
            $"PCL 未找到 {javaName}。\n" +
            $"请自行搜索并安装 {javaName}，安装后在 设置 → 启动选项 → 游戏 Java 中重新搜索或导入。",
            "未找到 Java");
    }

    /// <summary>
    /// 显示自动下载确认对话框
    /// </summary>
    private static bool ShowAutoDownloadConfirmation(string javaName) {
        return MsgBoxWrapper.Show(
            $"PCL 未找到 {javaName}，是否需要 PCL 自动下载？\n" +
            $"如果你已经安装了 {javaName}，可以在 设置 → 启动选项 → 游戏 Java 中手动导入。",
            "自动下载 Java？",
            buttons: ["自动下载", "取消"]) == 1;
    }

    /// <summary>
    /// 获取Java版本要求范围
    /// </summary>
    private (Version minVersion, Version maxVersion) GetJavaVersionRequirements() {
        return InstanceJavaService.GetCompatibleJavaVersionRange(
            instance,
            ((IJsonBasedInstance)instance).VersionJson!,
            ((IJsonBasedInstance)instance).VersionJsonInJar);
    }

    /// <summary>
    /// 获取实例指定的Java
    /// </summary>
    private JavaInfo? GetInstanceSpecifiedJava() {
        var userSetupVersion = Config.Instance.SelectedJava[instance.Path];
        return userSetupVersion == USE_GLOBAL_SETTING ? null : JavaInfo.Parse(userSetupVersion);
    }

    /// <summary>
    /// 获取全局指定的Java
    /// </summary>
    private static JavaInfo? GetGlobalSpecifiedJava() {
        return JavaInfo.Parse(Config.Launch.SelectedJava);
    }

    /// <summary>
    /// 自动搜索合适的Java
    /// </summary>
    private static async Task<JavaInfo?> SearchSuitableJavaAsync(Version minVersion, Version maxVersion) {
        var javaManager = JavaService.JavaManager;
        javaManager.CheckJavaAvailability();

        var suitableJava = (await javaManager.SelectSuitableJava(minVersion, maxVersion)).FirstOrDefault();

        if (suitableJava == null) {
            LogWrapper.Info("首次搜索未找到合适Java，重新扫描后再试");
            await javaManager.ScanJavaAsync();
            suitableJava = (await javaManager.SelectSuitableJava(minVersion, maxVersion)).FirstOrDefault();
        }

        LogWrapper.Info($"自动搜索结果：{suitableJava?.ToString() ?? "未找到"}");
        return suitableJava;
    }

    /// <summary>
    /// 检查Java版本是否合适
    /// </summary>
    private static bool IsJavaVersionSuitable(Version javaVersion, Version minVersion, Version maxVersion) {
        return javaVersion >= minVersion && javaVersion <= maxVersion;
    }

    /// <summary>
    /// Java规格定义
    /// </summary>
    private readonly struct JavaSpecification(string displayName, string code, bool requiresManualDownload) {
        public string DisplayName { get; } = displayName;
        public string Code { get; } = code;
        public bool RequiresManualDownload { get; } = requiresManualDownload;
    }
}

/// <summary>
/// JavaSelectService的静态工厂类，用于简化使用
/// </summary>
public static class JavaSelectServiceFactory {
    /// <summary>
    /// 为当前实例创建JavaSelectService并选择最佳Java
    /// </summary>
    /// <returns>选择的Java信息</returns>
    public static async Task<JavaInfo> SelectBestJavaForCurrentInstanceAsync() {
        var currentInstance = FolderService.FolderManager.CurrentInst!;

        var service = new JavaSelectService(currentInstance);
        return await service.SelectBestJavaAsync();
    }
}
