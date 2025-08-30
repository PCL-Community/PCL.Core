using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using fNbt;

namespace PCL.Core.Minecraft;

/// <summary>
/// 提供 NBT 文件的异步读写操作。
/// </summary>
public static class NbtFileHandler {
    /// <summary>
    /// 异步读取 NBT 文件。
    /// </summary>
    /// <param name="filePath">目标文件路径（完整或相对）。</param>
    /// <param name="tagName">要读取的 NbtList 的标签名称。</param>
    /// <param name="cancelToken">取消操作的令牌。</param>
    /// <returns>一个 NbtList 对象，如果文件或标签不存在则返回 null。</returns>
    public static async Task<NbtList?> ReadNbtFileAsync(string filePath, string tagName, CancellationToken cancelToken = default) {
        try {
            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath)) {
                LogWrapper.Warn($"NBT 文件不存在：{fullPath}");
                return null;
            }

            const int bufferSize = 4096;
            var nbtFile = new NbtFile();
            await Task.Run(async () => {
                await using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous);
                nbtFile.LoadFromStream(fs, NbtCompression.AutoDetect);
            }, cancelToken);

            var result = nbtFile.RootTag.Get<NbtList>(tagName);
            if (result == null) {
                LogWrapper.Warn($"未找到指定的 NBT 标签：{tagName}");
            }

            return result;
        } catch (OperationCanceledException) {
            LogWrapper.Info($"读取 NBT 文件操作被取消：{filePath}");
            return null;
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"读取 NBT 文件出错：{filePath}");
            return null;
        }
    }

    /// <summary>
    /// 异步写入 NBT 文件。
    /// </summary>
    /// <param name="nbtList">要写入文件的 NbtList 对象。</param>
    /// <param name="filePath">目标文件路径（完整或相对）。</param>
    /// <param name="compression">NBT 文件的压缩类型，默认为 NbtCompression.None。</param>
    /// <param name="cancelToken">取消操作的令牌。</param>
    /// <returns>返回操作是否成功。</returns>
    public static async Task<bool> WriteNbtFileAsync(NbtList nbtList, string filePath, NbtCompression compression = NbtCompression.None, CancellationToken cancelToken = default) {
        try {
            var fullPath = Path.GetFullPath(filePath);
            var directoryName = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directoryName)) {
                LogWrapper.Warn($"无法获取目标目录：{fullPath}");
                return false;
            }

            Directory.CreateDirectory(directoryName);

            var rootTag = new NbtCompound { Name = "" };
            rootTag.Add(nbtList);
            var nbtFile = new NbtFile(rootTag);

            const int bufferSize = 4096;
            await Task.Run(async () => {
                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous);
                nbtFile.SaveToStream(fs, compression);
            }, cancelToken);

            LogWrapper.Info($"NBT 文件成功保存于：{fullPath}");
            return true;
        } catch (OperationCanceledException) {
            LogWrapper.Info($"写入 NBT 文件操作被取消：{filePath}");
            return false;
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"写入 NBT 文件出错：{filePath}");
            return false;
        }
    }
}
