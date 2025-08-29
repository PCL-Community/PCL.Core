using PCL.Core.Logging;

namespace PCL.Core.Utils.Hash;

using System;
using System.IO;
using System.Threading;

public static class FileHashUtils {
    /// <summary>
    /// 计算文件的指定哈希值。
    /// </summary>
    /// <param name="filePath">要计算哈希的文件路径。</param>
    /// <param name="hashProvider">哈希算法提供者（如 MD5Provider、SHA1Provider 等）。</param>
    /// <param name="ignoreOnDownloading">是否忽略下载中的文件。</param>
    /// <returns>哈希值（十六进制字符串），失败时返回空字符串。</returns>
    public static string ComputeFileHash(string? filePath, IHashProvider hashProvider, bool ignoreOnDownloading = false) {
        if (string.IsNullOrEmpty(filePath)) {
            LogWrapper.Warn(new ArgumentNullException(nameof(filePath)), "文件路径为空");
            return string.Empty;
        }

        // 检查文件是否正在下载
        if (ignoreOnDownloading && IsFileDownloading(filePath)) {
            return string.Empty;
        }

        for (var attempt = 0; attempt < 2; attempt++) {
            try {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return hashProvider.ComputeHash(fs);
            } catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) {
                LogWrapper.Warn(ex, $"计算文件哈希失败：{filePath}");
                return string.Empty;
            } catch (Exception ex) {
                if (attempt == 0) {
                    LogWrapper.Warn(ex, $"计算文件哈希可重试失败：{filePath}");
                    Thread.Sleep(Random.Shared.Next(200, 500));
                    continue;
                }
                LogWrapper.Warn(ex, $"计算文件哈希失败：{filePath}");
                return string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// 获取文件的 MD5 哈希值。
    /// </summary>
    public static string GetFileMD5(string? filePath) => ComputeFileHash(filePath, MD5Provider.Instance);

    /// <summary>
    /// 获取文件的 SHA1 哈希值。
    /// </summary>
    public static string GetFileSHA1(string? filePath) => ComputeFileHash(filePath, SHA1Provider.Instance);

    /// <summary>
    /// 获取文件的 SHA256 哈希值。
    /// </summary>
    public static string GetFileSHA256(string? filePath, bool ignoreOnDownloading = false)
        => ComputeFileHash(filePath, SHA256Provider.Instance, ignoreOnDownloading);

    /// <summary>
    /// 获取文件的 SHA512 哈希值。
    /// </summary>
    public static string GetFileSHA512(string? filePath, bool ignoreOnDownloading = false)
        => ComputeFileHash(filePath, SHA512Provider.Instance, ignoreOnDownloading);

    private static bool IsFileDownloading(string filePath) {
        try {
            // Check if file exists
            if (!File.Exists(filePath)) {
                return false;
            }

            // Attempt to open the file with exclusive access
            using (File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                // If we can open the file, it's not being downloaded
                return false;
            }
        } catch (IOException) {
            // If we get an IOException, the file is likely being used/downloaded
            return true;
        } catch {
            // Handle other potential exceptions, but assume file is not downloading
            return false;
        }
    }
}
