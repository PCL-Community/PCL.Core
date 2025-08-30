using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using PCL.Core.IO;
using PCL.Core.UI;

namespace PCL.Core.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public static class LogManager {
    public static async Task<bool> ExportLogAsync(IEnumerable<string> sourceFiles, CancellationToken cancelToken = default) {
        const string filter = "PCL CE 日志压缩包|*.zip";
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var baseName = $"PCL_CE_Logs_{DateTime.Now:yyyyMMddHHmmss}";
        var tempDirName = $"{baseName}.tmp";
        var fileName = $"{baseName}.zip";
        var selectedPath = SystemDialogs.SelectSaveFile("导出日志文件", fileName, filter, desktopPath);

        if (string.IsNullOrEmpty(selectedPath)) {
            return false;
        }

        try {
            LogWrapper.Info("Log", $"开始日志导出至 {selectedPath}");
            Directory.CreateDirectory(tempDirName);

            if (File.Exists(selectedPath)) {
                File.Delete(selectedPath);
                LogWrapper.Info("Log", $"删除在 {selectedPath} 的已有文件");
            }

            await using var fileStream = new FileStream(selectedPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            await using var zipStream = new ZipOutputStream(fileStream);
            foreach (var item in sourceFiles) {
                var itemFileName = Path.GetFileName(item);
                var tempPath = Path.Combine(tempDirName, itemFileName);

                await Files.CopyFileAsync(item, tempPath, cancelToken);
                await using (var sourceStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) {
                    var entry = new ZipEntry(itemFileName);
                    await zipStream.PutNextEntryAsync(entry, cancelToken);
                    await sourceStream.CopyToAsync(zipStream, cancelToken);
                }
                File.Delete(tempPath);
            }
            await zipStream.FinishAsync(cancelToken);
            LogWrapper.Info("Log", $"日志导出完成: {selectedPath}");
            
            return true;
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "Log", "日志导出失败");
            return false;
        } finally {
            if (Directory.Exists(tempDirName)) {
                Directory.Delete(tempDirName, true);
                LogWrapper.Debug("Log", $"清理临时文件: {tempDirName}");
            }
        }
    }
}
