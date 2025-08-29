namespace PCL.Core.IO;

using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.BZip2;
using Logging;

public static class FileCompressionUtils {
    /// <summary>
    /// 尝试根据文件后缀名判断文件种类并解压，支持 zip、gz、tar、tar.gz 和 bzip2。
    /// 会尝试将 jar 文件以 zip 方式解压。不会清空目标文件夹，但会创建。
    /// </summary>
    /// <param name="compressFilePath">压缩文件的路径。</param>
    /// <param name="destDirectory">解压目标目录。</param>
    /// <param name="encoding">用于解压 zip 的编码，默认为 GB18030。</param>
    /// <param name="progressIncrementHandler">进度更新回调，接收 0 到 1 的进度值。</param>
    public static void ExtractFile(string? compressFilePath, string? destDirectory, Encoding? encoding = null, Action<double>? progressIncrementHandler = null) {
        if (string.IsNullOrEmpty(compressFilePath)) {
            LogWrapper.Error(new ArgumentNullException(nameof(compressFilePath)), "压缩文件路径为空");
            return;
        }

        if (string.IsNullOrEmpty(destDirectory)) {
            LogWrapper.Error(new ArgumentNullException(nameof(destDirectory)), "目标目录路径为空");
            return;
        }

        try {
            Directory.CreateDirectory(destDirectory);

            if (compressFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
                compressFilePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) {
                ExtractGZip(compressFilePath, destDirectory, progressIncrementHandler);
            } else if (compressFilePath.EndsWith(".bz2", StringComparison.OrdinalIgnoreCase)) {
                ExtractBZip2(compressFilePath, destDirectory, progressIncrementHandler);
            } else if (compressFilePath.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)) {
                ExtractTar(compressFilePath, destDirectory, progressIncrementHandler);
            } else if (compressFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                       compressFilePath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) {
                ExtractZip(compressFilePath, destDirectory, progressIncrementHandler);
            } else {
                LogWrapper.Error(new NotSupportedException("不支持的压缩文件格式"), $"文件 {compressFilePath} 的格式不受支持");
            }
        } catch (Exception ex) {
            LogWrapper.Error(ex, $"解压文件 {compressFilePath} 失败");
            throw;
        }
    }

    /// <summary>
    /// 解压 GZip 文件（包括 .gz 和 .tgz）。
    /// </summary>
    private static void ExtractGZip(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler) {
        var outputFileName = Path.GetFileName(compressFilePath).ToLower().Replace(".tar", "").Replace(".gz", "").Replace(".tgz", "");
        var outputPath = Path.Combine(destDirectory, outputFileName);

        using FileStream compressedFile = new(compressFilePath, FileMode.Open, FileAccess.Read);
        using GZipInputStream gzipStream = new(compressedFile);
        if (compressFilePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) {
            // 处理 .tgz（tar.gz）文件
            using TarInputStream tarStream = new(gzipStream, Encoding.UTF8);
            ExtractTarStream(tarStream, destDirectory, progressIncrementHandler);
        } else {
            // 处理普通 .gz 文件
            using FileStream outputStream = new(outputPath, FileMode.OpenOrCreate, FileAccess.Write);
            gzipStream.CopyTo(outputStream);
            progressIncrementHandler?.Invoke(1.0);
        }
    }

    /// <summary>
    /// 解压 BZip2 文件。
    /// </summary>
    private static void ExtractBZip2(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler) {
        var outputFileName = Path.GetFileName(compressFilePath).ToLower().Replace(".bz2", "");
        var outputPath = Path.Combine(destDirectory, outputFileName);

        using FileStream compressedFile = new(compressFilePath, FileMode.Open, FileAccess.Read);
        using BZip2InputStream bzip2Stream = new(compressedFile);
        using FileStream outputStream = new(outputPath, FileMode.OpenOrCreate, FileAccess.Write);
        bzip2Stream.CopyTo(outputStream);
        progressIncrementHandler?.Invoke(1.0);
    }

    /// <summary>
    /// 解压 Tar 文件。
    /// </summary>
    private static void ExtractTar(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler) {
        using FileStream compressedFile = new(compressFilePath, FileMode.Open, FileAccess.Read);
        using TarInputStream tarStream = new(compressedFile, Encoding.UTF8);
        ExtractTarStream(tarStream, destDirectory, progressIncrementHandler);
    }

    /// <summary>
    /// 解压 Tar 流中的内容。
    /// </summary>
    private static void ExtractTarStream(TarInputStream tarStream, string destDirectory, Action<double>? progressIncrementHandler) {
        TarEntry entry;
        var totalEntries = 0;
        while (tarStream.GetNextEntry() != null) {
            totalEntries++;
        }
        tarStream.Reset();

        var currentEntry = 0;
        while ((entry = tarStream.GetNextEntry()) != null) {
            var destinationPath = Path.Combine(destDirectory, entry.Name);
            if (entry.IsDirectory) {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            using FileStream outputStream = new(destinationPath, FileMode.OpenOrCreate, FileAccess.Write);
            tarStream.CopyEntryContents(outputStream);
            currentEntry++;
            progressIncrementHandler?.Invoke((double)currentEntry / totalEntries);
        }
    }

    /// <summary>
    /// 解压 Zip 文件（包括 .zip 和 .jar）。
    /// </summary>
    private static void ExtractZip(string compressFilePath, string destDirectory, Action<double>? progressIncrementHandler) {
        using ZipFile zipFile = new(compressFilePath);

        var totalEntries = zipFile.Count;
        long currentEntry = 0;

        foreach (ZipEntry entry in zipFile) {
            var destinationPath = Path.Combine(destDirectory, entry.Name);
            if (entry.IsDirectory) {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            using var zipStream = zipFile.GetInputStream(entry);
            using FileStream outputStream = new(destinationPath, FileMode.OpenOrCreate, FileAccess.Write);
            zipStream.CopyTo(outputStream);
            currentEntry++;
            progressIncrementHandler?.Invoke((double)currentEntry / totalEntries);
        }
    }
}
