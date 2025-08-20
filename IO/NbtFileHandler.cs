﻿using System;
using PCL.Core.Logging;

namespace PCL.Core.IO;

using fNbt;
using System.IO;

/// <summary>
/// 提供 NBT 文件的读写操作。
/// </summary>
public static class NbtFileHandler
{
    /// <summary>
    /// 简化的 NBT 文件读取方法。
    /// </summary>
    /// <param name="filePath">目标文件路径。</param>
    /// <param name="tagName">要读取的 NbtList 的标签名称。</param>
    /// <returns>一个 NbtList 对象，如果文件或标签不存在则返回 null。</returns>
    public static NbtList ReadNbtFile(string filePath, string tagName) {
        // 确保文件存在
        if (!File.Exists(filePath))
        {
            return null;
        }

        try {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var nbtFile = new NbtFile();
                nbtFile.LoadFromStream(fs, NbtCompression.AutoDetect);

                // 根据标签名称获取 NbtList
                return nbtFile.RootTag.Get<NbtList>(tagName);
            }
        }
        catch (Exception ex)
        {
            // 处理读取异常，例如文件损坏或格式错误
            LogWrapper.Warn($"Error reading NBT file: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 简化的 NBT 文件写入方法。
    /// </summary>
    /// <param name="nbtList">要写入文件的 NbtList 对象。</param>
    /// <param name="filePath">目标文件路径。</param>
    public static void WriteNbtFile(NbtList nbtList, string filePath) {
        // 创建一个根节点（TAG_Compound）
        var rootTag = new NbtCompound();
        rootTag.Name = "";

        // 添加 NbtList 到根节点
        rootTag.Add(nbtList);

        // 创建 NbtFile 实例
        var nbtFile = new NbtFile(rootTag);

        try {
            // 保存文件
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
                nbtFile.SaveToStream(fs, NbtCompression.GZip);
            }
            LogWrapper.Info($"NBT file saved successfully at: {filePath}");
        } catch (Exception ex) {
            LogWrapper.Warn($"Error writing NBT file: {ex.Message}");
        }
    }
}