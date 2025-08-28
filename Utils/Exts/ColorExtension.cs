using System.Windows.Media;
using PCL.Core.UI;

namespace PCL.Core.Utils.Exts;

/// <summary>
/// 使用示例和扩展方法
/// </summary>
public static class ColorExtensions {
    /// <summary>
    /// 为WPF Color添加现代化操作
    /// </summary>
    public static ModernColor ToModern(this Color color) => new(color);

    /// <summary>
    /// 为字符串添加颜色解析
    /// </summary>
    public static ModernColor ToColor(this string hexString) => new(hexString);
}

