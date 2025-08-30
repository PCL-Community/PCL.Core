using PCL.Core.IO;

namespace PCL.Core.Utils.OS;

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

public class ClipboardUtils {
    /// <summary>
    /// 将剪贴板内容设置为用于复制/粘贴操作的文件或文件夹路径列表。
    /// </summary>
    /// <param name="paths">要设置到剪贴板的文件或文件夹路径数组。</param>
    public void SetClipboardFiles(string[] paths) {
        if (paths == null || paths.Length == 0) {
            throw new ArgumentException("Paths cannot be null or empty.", nameof(paths));
        }

        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.FileDrop, paths);
        Clipboard.SetDataObject(dataObject);
    }

    /// <summary>
    /// 将剪贴板内容设置为指定文本。
    /// </summary>
    /// <param name="text">要设置到剪贴板的文本内容。</param>
    /// <exception cref="ArgumentNullException">当 text 为空时抛出此异常。</exception>
    public void SetClipboardText(string text) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text), "Text cannot be null.");
        }

        Clipboard.SetText(text);
    }

    /// <summary>
    /// 从剪切板粘贴文件或文件夹
    /// </summary>
    /// <param name="dest">目标文件夹</param>
    /// <param name="copyFile">是否粘贴文件</param>
    /// <param name="copyDir">是否粘贴文件夹</param>
    /// <returns>总共粘贴的数量</returns>
    public async Task<int> PasteFromClipboardAsync(string dest, bool copyFile, bool copyDir) {
        if (string.IsNullOrEmpty(dest)) {
            throw new ArgumentException("Destination folder cannot be null or empty.", nameof(dest));
        }

        if (!Directory.Exists(dest)) {
            Directory.CreateDirectory(dest);
        }

        var dataObject = Clipboard.GetDataObject();
        if (dataObject == null || !dataObject.GetDataPresent(DataFormats.FileDrop)) {
            return 0;
        }

        var data = dataObject.GetData(DataFormats.FileDrop);
        if (data is not string[] paths) {
            return 0;
        }
        if (paths.Length == 0) {
            return 0;
        }

        var count = 0;
        foreach (var path in paths) {
            if (File.Exists(path) && copyFile) {
                var targetPath = Path.Combine(dest, Path.GetFileName(path));
                await Files.CopyFileAsync(path, targetPath);
                count++;
            } else if (Directory.Exists(path) && copyDir) {
                var targetDir = Path.Combine(dest, new DirectoryInfo(path).Name);
                await Files.CopyDirectoryAsync(path, targetDir);
                count++;
            }
        }

        return count;
    }
}
