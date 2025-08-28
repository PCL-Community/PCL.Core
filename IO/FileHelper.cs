using PCL.Core.Logging;

namespace PCL.Core.IO;

using System;
using System.IO;
using System.Security;

/// <summary>
/// 提供与文件和文件夹 I/O 权限相关的实用方法。
/// </summary>
public static class FileHelper {
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
            throw new ArgumentNullException("文件夹名不能为空！");
        }
        if (!Directory.Exists(path)) {
            throw new DirectoryNotFoundException("文件夹不存在！");
        }

        // 核心逻辑：创建和删除临时文件
        var tempFileName = Path.Combine(path, "CheckPermission");
        using (File.Create(tempFileName)) { }
        File.Delete(tempFileName);
    }
}