using System;
using System.Windows;
using System.Windows.Media;
using PCL.Core.Logging;

namespace PCL.Core.UI;

public static class FontManager {
    private const string DefaultFontList = "Segoe UI, Microsoft YaHei UI";
    private const string ResourceFontPath = "pack://application:,,,/Resources/#PCL English";

    /// <summary>
    /// 设置 WPF 应用程序的字体资源（LaunchFontFamily）。
    /// </summary>
    /// <param name="fontName">首选字体名称（可选）。若为 null 或空，使用默认字体（PCL English, Segoe UI, Microsoft YaHei UI）。</param>
    public static void SetLaunchFont(string? fontName = null) {
        try {
            var fontFamily = string.IsNullOrEmpty(fontName)
                ? new FontFamily(new Uri(ResourceFontPath, UriKind.Absolute), $"./Resources/#PCL English, {DefaultFontList}")
                : new FontFamily($"{fontName}, {DefaultFontList}");

            Application.Current.Resources["LaunchFontFamily"] = fontFamily ?? throw new InvalidOperationException("字体资源创建失败");
        } catch (Exception ex) when (ex is UriFormatException or InvalidOperationException) {
            LogWrapper.Warn(ex, "设置字体失败");
            // 回退到系统默认字体
            Application.Current.Resources["LaunchFontFamily"] = new FontFamily(DefaultFontList);
        }
    }
}
