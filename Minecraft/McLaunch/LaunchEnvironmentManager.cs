using System;
using System.IO;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Minecraft.McLaunch;

public static class LaunchEnvironmentManager {
    private const string JavaWrapperResource = "Resources/java-wrapper.jar";
    private const string LinkDResource = "Resources/linkd.exe";

    private static readonly object ExtractJavaWrapperLock = new();
    private static readonly object ExtractLinkDLock = new();

    public static string ExtractJavaWrapper() => ExtractFile(JavaWrapperResource, "JavaWrapper.jar", ExtractJavaWrapperLock);
    public static string ExtractLinkD() => ExtractFile(LinkDResource, "linkd.exe", ExtractLinkDLock);

    private static string ExtractFile(string resourceName, string fileName, object lockObj) {
        var filePath = Path.Combine(FileService.TempPath, fileName);
        LogWrapper.Info(resourceName, $"选定路径：{filePath}");

        lock (lockObj) {
            try {
                WriteResourceToFile(resourceName, filePath);
            } catch (Exception ex) {
                if (File.Exists(filePath)) {
                    LogWrapper.Warn(ex, $"{resourceName} 文件释放失败，尝试删除后重试");
                    File.Delete(filePath);
                    try {
                        WriteResourceToFile(resourceName, filePath);
                    } catch (Exception ex2) {
                        var fallbackPath = Path.Combine(FileService.TempPath, $"{Path.GetFileNameWithoutExtension(fileName)}2{Path.GetExtension(fileName)}");
                        LogWrapper.Warn(ex2, $"{resourceName} 重试失败，尝试新路径：{fallbackPath}");
                        WriteResourceToFile(resourceName, fallbackPath);
                        filePath = fallbackPath;
                    }
                } else {
                    throw new FileNotFoundException($"释放 {resourceName} 失败", ex);
                }
            }
        }
        return filePath;
    }

    private static void WriteResourceToFile(string resourceName, string path) {
        using var sourceStream = Basics.GetResourceStream(resourceName);
        if (sourceStream == null)
            throw new FileNotFoundException($"资源 {resourceName} 未找到。");

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
        sourceStream.CopyTo(fileStream);
    }

    private static bool McLaunchNeedsRetroWrapper(McInstance.McInstance mcInstance) {
        var versionInfo = mcInstance.GetVersionInfo();
        if (versionInfo == null) return false;

        var isOldVersion = versionInfo.McVersionMinor < 6 && versionInfo.McVersionMinor != 99;
        var isSpecificVersion = versionInfo.ReleaseTime >= new DateTime(2013, 6, 25) && versionInfo.McVersionMinor == 99;
        var isRwEnabled = !Setup.Launch.DisableRw && !Setup.Instance.DisableRw[mcInstance.Path];

        return (isOldVersion || isSpecificVersion) && isRwEnabled;
    }
}
