using PCL.Core.Logging;

namespace PCL.Core.IO.FileFormats;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

public static class IniFileHandler {
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> IniCache = new();
    private static readonly object WriteIniLock = new();

    /// <summary>
    /// 获取完整的 INI 文件路径。如果是简写文件名，补全为 "Paths.Path\PCL\文件名.ini"。
    /// </summary>
    private static string GetFullPath(string fileName) {
        ArgumentNullException.ThrowIfNull(fileName);
        return fileName.Contains(":\\") ? fileName : Path.Combine(Paths.Path, "PCL", $"{fileName}.ini");
    }

    /// <summary>
    /// 清除指定 INI 文件的运行时缓存。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    public static void IniClearCache(string fileName) {
        IniCache.TryRemove(GetFullPath(fileName), out _);
    }

    /// <summary>
    /// 获取 INI 文件内容（从缓存或文件读取）。文件不存在或读取失败时返回 null。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    private static ConcurrentDictionary<string, string>? IniGetContent(string fileName) {
        try {
            string fullPath = GetFullPath(fileName);
            return IniCache.GetOrAdd(fullPath, _ => {
                if (!File.Exists(fullPath)) return null;
                var ini = new ConcurrentDictionary<string, string>();
                string content = Files.ReadFile(fullPath); // 使用 FileManager.ReadFile
                foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
                    int index = line.IndexOf(':');
                    if (index > 0) {
                        string key = line[..index];
                        string value = line[(index + 1)..];
                        ini[key] = value; // 可能覆盖重复键，见 #3616
                    }
                }
                return ini;
            });
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"生成 INI 文件缓存失败（{fileName}）");
            return null;
        }
    }

    /// <summary>
    /// 读取 INI 文件中指定键的值（可能使用缓存）。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    /// <param name="key">要读取的键。</param>
    /// <param name="defaultValue">键不存在时的默认值。</param>
    /// <returns>键对应的值，或默认值。</returns>
    public static string ReadIni(string fileName, string key, string defaultValue = "") {
        ArgumentNullException.ThrowIfNull(key);
        var content = IniGetContent(fileName);
        return content != null && content.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// 检查 INI 文件中是否包含指定键（可能使用缓存）。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    /// <param name="key">要检查的键。</param>
    /// <returns>如果键存在，返回 true；否则返回 false。</returns>
    public static bool HasIniKey(string fileName, string key) {
        ArgumentNullException.ThrowIfNull(key);
        var content = IniGetContent(fileName);
        return content != null && content.ContainsKey(key);
    }

    /// <summary>
    /// 从 INI 文件中移除指定键（更新缓存和文件）。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    /// <param name="key">要移除的键。</param>
    public static void DeleteIniKey(string fileName, string key) {
        IniFileHandler.WriteIni(fileName, key, null);
    }

    /// <summary>
    /// 写入 INI 文件（更新缓存和文件）。如果值为空，则删除该键。
    /// </summary>
    /// <param name="fileName">文件完整路径或简写文件名。简写将使用 "Paths.Path\PCL\文件名.ini"。</param>
    /// <param name="key">要写入的键。</param>
    /// <param name="value">要写入的值，或 null 表示删除键。</param>
    /// <exception cref="ArgumentException">当键包含冒号时抛出。</exception>
    public static void WriteIni(string fileName, string key, string? value) {
        try {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Contains(':')) throw new ArgumentException($"键名中包含冒号：{key}", nameof(key));

            string cleanedKey = key.Replace("\r", "").Replace("\n", "");
            string? cleanedValue = value?.Replace("\r", "").Replace("\n", "");

            lock (WriteIniLock) {
                string fullPath = GetFullPath(fileName);
                var content = IniGetContent(fileName) ?? new ConcurrentDictionary<string, string>();

                if (cleanedValue == null) {
                    if (!content.ContainsKey(cleanedKey)) return;
                    content.TryRemove(cleanedKey, out _);
                } else {
                    if (content.TryGetValue(cleanedKey, out var existingValue) && existingValue == cleanedValue) return;
                    content[cleanedKey] = cleanedValue;
                }

                string fileContent = string.Join("\n", content.Select(pair => $"{pair.Key}:{pair.Value}"));
                Files.WriteFile(fullPath, fileContent); // 使用 FileManager.WriteFile
                IniCache[fullPath] = content;
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, $"写入 INI 文件失败（{fileName} → {key}:{value})");
        }
    }
}
