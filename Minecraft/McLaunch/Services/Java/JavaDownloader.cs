namespace PCL.Core.Minecraft.McLaunch.Services.Java;

using System;
using System.Threading.Tasks;

/// <summary>
/// Java下载器
/// </summary>
public static class JavaDownloader {
    /// <summary>
    /// 下载指定版本的Java
    /// </summary>
    public static async Task<Result<Java>> DownloadJavaAsync(string javaCode) {
        try {
            return await Task.Run(() => DownloadJava(javaCode));
        } catch (Exception ex) {
            return Result<Java>.Failed($"下载Java失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 确认是否下载Java
    /// </summary>
    public static bool ConfirmDownload(string javaVersionText, bool isLegacy = false) {
        try {
            return JavaDownloadConfirm(javaVersionText, isLegacy);
        } catch (Exception ex) {
            Log($"Java下载确认失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取Java下载加载器
    /// </summary>
    public static LoaderBase GetJavaLoader(string javaCode) {
        try {
            return JavaFixLoaders(javaCode);
        } catch (Exception ex) {
            Log($"获取Java加载器失败: {ex.Message}");
            return null;
        }
    }

    private static Result<Java> DownloadJava(string javaCode) {
        // 获取Java下载加载器
        var javaLoader = GetJavaLoader(javaCode);
        if (javaLoader == null)
            return Result<Java>.Failed($"无法获取Java {javaCode}的下载加载器");

        try {
            // 启动下载
            javaLoader.Start(javaCode, IsForceRestart: true);

            // 等待下载完成
            while (javaLoader.State == LoadState.Loading) {
                System.Threading.Thread.Sleep(100);
            }

            // 检查下载结果
            if (javaLoader.State == LoadState.Finished) {
                // 下载成功，重新检查可用的Java
                var availableJava = JavaSelect("$$", new Version(0, 0), new Version(999, 999), null);
                if (availableJava != null)
                    return Result<Java>.Success(availableJava);
            }

            return Result<Java>.Failed($"Java {javaCode}下载失败或未找到下载后的Java");
        } catch (Exception ex) {
            return Result<Java>.Failed($"Java {javaCode}下载过程中出现异常", ex);
        } finally {
            javaLoader?.Abort(); // 确保清理资源
        }
    }

    /// <summary>
    /// 根据版本要求确定需要下载的Java版本代码
    /// </summary>
    public static string DetermineJavaCode(Version minVersion, Version maxVersion, McInstance instance) {
        if (minVersion >= new Version(22, 0)) {
            return minVersion.Major.ToString();
        } else if (minVersion >= new Version(21, 0)) {
            return "21";
        } else if (minVersion >= new Version(1, 9)) {
            return "17";
        } else if (maxVersion < new Version(1, 8)) {
            return "7";
        } else if (minVersion > new Version(1, 8, 0, 140) && maxVersion < new Version(1, 8, 0, 321)) {
            return "8u141";
        } else if (minVersion > new Version(1, 8, 0, 140)) {
            return "8u141";
        } else if (maxVersion < new Version(1, 8, 0, 321)) {
            return "8";
        } else {
            return "8"; // 默认Java 8
        }
    }

    /// <summary>
    /// 获取Java版本的友好显示名称
    /// </summary>
    public static string GetJavaDisplayName(string javaCode) {
        return javaCode switch {
            "7" => "Java 7",
            "8" => "Java 8",
            "8u141" => "Java 8.0.141+",
            "17" => "Java 17",
            "21" => "Java 21",
            _ when int.TryParse(javaCode, out var version) => $"Java {version}",
            _ => $"Java {javaCode}"
        };
    }

    // 这些是从原VB代码中需要引用的方法和属性的占位符
    private static bool JavaDownloadConfirm(string javaVersionText, bool isLegacy = false) {
        throw new NotImplementedException("需要从原代码引用JavaDownloadConfirm方法");
    }

    private static LoaderBase JavaFixLoaders(string javaCode) {
        throw new NotImplementedException("需要从原代码引用JavaFixLoaders方法");
    }

    private static Java JavaSelect(string placeholder, Version minVer, Version maxVer, McInstance instance) {
        throw new NotImplementedException("需要从原代码引用JavaSelect方法");
    }

    private static void Log(string message) {
        throw new NotImplementedException("需要从原代码引用Log方法");
    }
}
