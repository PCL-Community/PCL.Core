using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using PCL.Core.Logging;

namespace PCL.Core.IO;

public static class Directories {
    /// <summary>
    /// 检查是否拥有对指定文件夹的 I/O 权限。
    /// 如果文件夹不存在，会返回 false。
    /// </summary>
    /// <param name="path">要检查的文件夹路径。</param>
    /// <returns>如果拥有权限且文件夹存在，则为 true；否则为 false。</returns>
    public static bool CheckPermission(string path) {
        try {
            if (string.IsNullOrWhiteSpace(path)) {
                return false;
            }

            // 检查一些系统特殊文件夹，这些文件夹通常没有权限
            if (path.EndsWith(":\\System Volume Information", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(":\\$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            // 检查文件夹是否存在
            if (!Directory.Exists(path)) {
                return false;
            }

            // 核心逻辑：通过创建和删除临时文件来检查权限
            var tempFileName = Path.Combine(path, Guid.NewGuid().ToString());
            using (File.Create(tempFileName)) { }
            File.Delete(tempFileName);

            return true;
        } catch (IOException) {
            return false;
        } catch (UnauthorizedAccessException) {
            return false;
        } catch (SecurityException) {
            return false;
        } catch (Exception ex) {
            // 捕获并记录其他未知异常
            LogWrapper.Warn(ex, $"没有对文件夹 {path} 的权限，请尝试以管理员权限运行。");
            return false;
        }
    }
    /// <summary>
    /// 检查是否拥有对指定文件夹的 I/O 权限。
    /// 如果出错，则抛出异常。
    /// </summary>
    /// <param name="path">要检查的文件夹路径。</param>
    /// <exception cref="ArgumentNullException">文件夹路径为空或只包含空格。</exception>
    /// <exception cref="DirectoryNotFoundException">文件夹不存在。</exception>
    /// <exception cref="UnauthorizedAccessException">没有访问文件夹的权限。</exception>
    public static void CheckPermissionWithException(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentNullException(nameof(path), "文件夹名不能为空！");
        }
        if (!Directory.Exists(path)) {
            throw new DirectoryNotFoundException("文件夹不存在！");
        }

        // 核心逻辑：创建和删除临时文件
        var tempFileName = Path.Combine(path, "CheckPermission");
        using (File.Create(tempFileName)) { }
        File.Delete(tempFileName);
    }
    /// <summary>
    /// 删除文件夹及其内容，返回删除的文件数。支持忽略错误。
    /// </summary>
    /// <param name="path">要删除的文件夹路径。</param>
    /// <param name="ignoreIssue">是否忽略删除过程中的错误。</param>
    /// <returns>成功删除的文件数。</returns>
    public static int DeleteDirectory(string? path, bool ignoreIssue = false) {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) {
            return 0;
        }

        var deletedCount = 0;

        try {
            // 枚举文件，延迟加载以提高性能
            foreach (var filePath in Directory.EnumerateFiles(path)) {
                for (var attempt = 0; attempt < 2; attempt++) {
                    try {
                        File.Delete(filePath);
                        deletedCount++;
                        break;
                    } catch (Exception ex) when (attempt == 0) {
                        LogWrapper.Error(ex, $"删除文件失败，将在 0.3s 后重试（{filePath}）");
                        Thread.Sleep(300);
                    } catch (Exception ex) {
                        if (ignoreIssue) {
                            LogWrapper.Error(ex, "删除单个文件可忽略地失败");
                        } else {
                            throw;
                        }
                    }
                }
            }

            // 递归删除子目录
            deletedCount += Directory.EnumerateDirectories(path).Sum(subDir => DeleteDirectory(subDir, ignoreIssue));

            // 删除空目录
            for (var attempt = 0; attempt < 2; attempt++) {
                try {
                    Directory.Delete(path, true);
                    break;
                } catch (Exception ex) when (attempt == 0) {
                    LogWrapper.Error(ex, $"删除文件夹失败，将在 0.3s 后重试（{path}）");
                    Thread.Sleep(300);
                } catch (Exception ex) {
                    if (ignoreIssue) {
                        LogWrapper.Error(ex, "删除单个文件夹可忽略地失败");
                    } else {
                        throw;
                    }
                }
            }
        } catch (DirectoryNotFoundException ex) {
            // 处理疑似符号链接的情况
            LogWrapper.Error(ex, $"疑似为孤立符号链接，尝试直接删除（{path}）", "Developer");
            try {
                Directory.Delete(path);
            } catch (Exception deleteEx) {
                if (!ignoreIssue) {
                    throw;
                }
                LogWrapper.Error(deleteEx, $"删除符号链接文件夹失败（{path}）");
            }
        }

        return deletedCount;
    }
    /// <summary>
    /// 复制文件夹及其内容，失败时抛出异常。
    /// </summary>
    /// <param name="fromPath">源文件夹路径。</param>
    /// <param name="toPath">目标文件夹路径。</param>
    /// <param name="progressIncrementHandler">进度更新回调，接收 0 到 1 的进度值。</param>
    public static void CopyDirectory(string? fromPath, string? toPath, Action<double>? progressIncrementHandler = null) {
        if (string.IsNullOrEmpty(fromPath)) {
            throw new ArgumentNullException(nameof(fromPath), "源文件夹路径为空");
        }

        if (string.IsNullOrEmpty(toPath)) {
            throw new ArgumentNullException(nameof(toPath), "目标文件夹路径为空");
        }

        // 规范化路径
        fromPath = Path.GetFullPath(fromPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        toPath = Path.GetFullPath(toPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var allFiles = EnumerateFiles(fromPath).ToList();
        var totalFiles = allFiles.Count;
        long copiedFiles = 0;

        foreach (var file in allFiles) {
            var relativePath = file.FullName[fromPath.Length..];
            var destFilePath = Path.Combine(toPath, relativePath);

            // 确保目标目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);

            for (var attempt = 0; attempt < 2; attempt++) {
                try {
                    File.Copy(file.FullName, destFilePath, overwrite: true);
                    copiedFiles++;
                    progressIncrementHandler?.Invoke((double)copiedFiles / totalFiles);
                    break;
                } catch (Exception ex) when (attempt == 0) {
                    LogWrapper.Error(ex, $"复制文件失败，将在 0.3s 后重试（{file.FullName} 到 {destFilePath}）");
                    Thread.Sleep(300);
                } catch (Exception ex) {
                    LogWrapper.Error(ex, $"复制文件失败（{file.FullName} 到 {destFilePath}）");
                    throw;
                }
            }
        }
    }
    /// <summary>
    /// 遍历文件夹中的所有文件。
    /// </summary>
    /// <param name="directory">要遍历的文件夹路径。</param>
    /// <returns>文件信息的枚举器。</returns>
    public static IEnumerable<FileInfo> EnumerateFiles(string? directory) {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) {
            LogWrapper.Error(new DirectoryNotFoundException($"目录不存在：{directory}"), "遍历文件夹失败");
            return [];
        }

        try {
            return new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories);
        } catch (Exception ex) {
            LogWrapper.Error(ex, $"遍历文件夹失败（{directory}）");
            return [];
        }
    }
}
