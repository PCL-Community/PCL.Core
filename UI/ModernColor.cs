namespace PCL.Core.UI;

using System;
using System.Windows.Media;

/// <summary>
/// 现代化的颜色类，支持小数精度和各种颜色操作
/// 使用不可变设计和显式转换提高可维护性
/// </summary>
public readonly record struct ModernColor {
    #region 属性和字段

    /// <summary>透明度 (0-255)</summary>
    public double A { get; init; }

    /// <summary>红色分量 (0-255)</summary>
    public double R { get; init; }

    /// <summary>绿色分量 (0-255)</summary>
    public double G { get; init; }

    /// <summary>蓝色分量 (0-255)</summary>
    public double B { get; init; }

    /// <summary>是否完全透明</summary>
    public bool IsTransparent => A <= 0;

    /// <summary>是否完全不透明</summary>
    public bool IsOpaque => A >= 255;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建RGB颜色 (不透明)
    /// </summary>
    public ModernColor(double r, double g, double b) : this(255, r, g, b) { }

    /// <summary>
    /// 创建ARGB颜色
    /// </summary>
    public ModernColor(double a, double r, double g, double b) {
        A = Math.Clamp(a, 0, 255);
        R = Math.Clamp(r, 0, 255);
        G = Math.Clamp(g, 0, 255);
        B = Math.Clamp(b, 0, 255);
    }

    /// <summary>
    /// 从WPF Color创建
    /// </summary>
    public ModernColor(Color color) : this(color.A, color.R, color.G, color.B) { }

    /// <summary>
    /// 从十六进制字符串创建
    /// </summary>
    public ModernColor(string hexString) {
        if (string.IsNullOrWhiteSpace(hexString))
            throw new ArgumentException("颜色字符串不能为空", nameof(hexString));

        try {
            var color = (Color)ColorConverter.ConvertFromString(hexString);
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        } catch (Exception ex) {
            throw new ArgumentException($"无效的颜色字符串: {hexString}", nameof(hexString), ex);
        }
    }

    /// <summary>
    /// 从SolidColorBrush创建
    /// </summary>
    public ModernColor(SolidColorBrush brush) {
        ArgumentNullException.ThrowIfNull(brush);
        var color = brush.Color;
        A = color.A;
        R = color.R;
        G = color.G;
        B = color.B;
    }

    #endregion

    #region 静态工厂方法

    /// <summary>常用颜色：白色</summary>
    public static ModernColor White => new(255, 255, 255);

    /// <summary>常用颜色：黑色</summary>
    public static ModernColor Black => new(0, 0, 0);

    /// <summary>常用颜色：透明</summary>
    public static ModernColor Transparent => new(0, 0, 0, 0);

    /// <summary>常用颜色：红色</summary>
    public static ModernColor Red => new(255, 0, 0);

    /// <summary>常用颜色：绿色</summary>
    public static ModernColor Green => new(0, 255, 0);

    /// <summary>常用颜色：蓝色</summary>
    public static ModernColor Blue => new(0, 0, 255);

    /// <summary>
    /// 从HSL创建颜色
    /// </summary>
    /// <param name="hue">色调 (0-360)</param>
    /// <param name="saturation">饱和度 (0-100)</param>
    /// <param name="lightness">亮度 (0-100)</param>
    public static ModernColor FromHsl(double hue, double saturation, double lightness) {
        var converter = new HslConverter();
        return converter.FromHsl(hue, saturation, lightness);
    }

    /// <summary>
    /// 从HSL创建颜色 (改进版本，对特定色调进行亮度调整)
    /// </summary>
    public static ModernColor FromHslEnhanced(double hue, double saturation, double lightness) {
        var converter = new HslConverter();
        return converter.FromHslEnhanced(hue, saturation, lightness);
    }

    /// <summary>
    /// 从对象创建颜色
    /// </summary>
    public static ModernColor FromObject(object? obj) => obj switch {
        null => White,
        Color color => new ModernColor(color),
        SolidColorBrush brush => new ModernColor(brush),
        ModernColor modernColor => modernColor,
        string str => new ModernColor(str),
        _ => throw new ArgumentException($"不支持的颜色类型: {obj.GetType()}")
    };
    
    /// <summary>
    /// 安全地从对象创建颜色
    /// </summary>
    public static ModernColor TryFromObject(object? obj) => obj switch {
        null => White,
        Color color => new ModernColor(color),
        SolidColorBrush brush => new ModernColor(brush),
        ModernColor modernColor => modernColor,
        string str => new ModernColor(str),
        _ => new ModernColor()
};

    #endregion

    #region 颜色运算操作符

    /// <summary>颜色加法</summary>
    public static ModernColor operator +(ModernColor left, ModernColor right) =>
        new(left.A + right.A, left.R + right.R, left.G + right.G, left.B + right.B);

    /// <summary>颜色减法</summary>
    public static ModernColor operator -(ModernColor left, ModernColor right) =>
        new(left.A - right.A, left.R - right.R, left.G - right.G, left.B - right.B);

    /// <summary>颜色缩放</summary>
    public static ModernColor operator *(ModernColor color, double factor) =>
        new(color.A * factor, color.R * factor, color.G * factor, color.B * factor);

    /// <summary>颜色缩放</summary>
    public static ModernColor operator *(double factor, ModernColor color) => color * factor;

    /// <summary>颜色除法</summary>
    public static ModernColor operator /(ModernColor color, double divisor) =>
        divisor == 0 ? throw new DivideByZeroException("不能除以零") : color * (1.0 / divisor);

    #endregion

    #region 显式转换操作符 (避免意外转换)

    /// <summary>显式转换为WPF Color</summary>
    public static explicit operator Color(ModernColor color) =>
        Color.FromArgb(_ToByte(color.A), _ToByte(color.R), _ToByte(color.G), _ToByte(color.B));

    /// <summary>显式转换为SolidColorBrush</summary>
    public static explicit operator SolidColorBrush(ModernColor color) =>
        new((Color)color);

    /// <summary>显式转换为System.Drawing.Color</summary>
    public static explicit operator System.Drawing.Color(ModernColor color) =>
        System.Drawing.Color.FromArgb(_ToByte(color.A), _ToByte(color.R), _ToByte(color.G), _ToByte(color.B));

    /// <summary>从WPF Color隐式转换 (常用转换保持隐式)</summary>
    public static implicit operator ModernColor(Color color) => new(color);

    /// <summary>从字符串隐式转换</summary>
    public static implicit operator ModernColor(string hexString) => new(hexString);

    #endregion

    #region 实用方法

    /// <summary>
    /// 创建具有指定透明度的新颜色
    /// </summary>
    public ModernColor WithAlpha(double alpha) => this with { A = Math.Clamp(alpha, 0, 255) };

    /// <summary>
    /// 线性插值到另一个颜色
    /// </summary>
    public ModernColor Lerp(ModernColor target, double t) {
        t = Math.Clamp(t, 0, 1);
        return new ModernColor(
            A + (target.A - A) * t,
            R + (target.R - R) * t,
            G + (target.G - G) * t,
            B + (target.B - B) * t
            );
    }

    /// <summary>
    /// 获取颜色的亮度 (0-1)
    /// </summary>
    public double GetBrightness() => (0.299 * R + 0.587 * G + 0.114 * B) / 255.0;

    /// <summary>
    /// 判断是否为暗色
    /// </summary>
    public bool IsDark() => GetBrightness() < 0.5;

    /// <summary>
    /// 获取对比色 (黑色或白色)
    /// </summary>
    public ModernColor GetContrastColor() => IsDark() ? White : Black;

    /// <summary>
    /// 转换为十六进制字符串
    /// </summary>
    public string ToHex(bool includeAlpha = false) {
        var color = (Color)this;
        return includeAlpha
            ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 安全地将double转换为byte
    /// </summary>
    private static byte _ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);

    #endregion

    #region 覆盖方法

    public override string ToString() => $"ARGB({A:F1}, {R:F1}, {G:F1}, {B:F1})";

    public string ToString(string format) => format.ToUpperInvariant() switch {
        "HEX" => ToHex(),
        "HEXA" => ToHex(true),
        "RGB" => $"RGB({R:F1}, {G:F1}, {B:F1})",
        _ => ToString()
    };

    #endregion
}

/// <summary>
/// HSL颜色转换器 - 分离职责，提高可测试性
/// </summary>
public sealed class HslConverter {
    /// <summary>
    /// 从HSL转换为RGB
    /// </summary>
    public ModernColor FromHsl(double hue, double saturation, double lightness) {
        // 参数验证和范围限制
        hue = ((hue % 360) + 360) % 360; // 确保在0-360范围内
        saturation = Math.Clamp(saturation, 0, 100);
        lightness = Math.Clamp(lightness, 0, 100);

        if (saturation == 0) {
            // 灰度色
            var gray = lightness * 2.55;
            return new ModernColor(gray, gray, gray);
        }

        var h = hue / 360.0;
        var s = saturation / 100.0;
        var l = lightness / 100.0;

        var v2 = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var v1 = 2 * l - v2;

        var r = 255 * _HueToRgb(v1, v2, h + 1.0 / 3.0);
        var g = 255 * _HueToRgb(v1, v2, h);
        var b = 255 * _HueToRgb(v1, v2, h - 1.0 / 3.0);

        return new ModernColor(r, g, b);
    }

    /// <summary>
    /// HSL转换的改进版本，对特定色调进行亮度调整
    /// </summary>
    public ModernColor FromHslEnhanced(double hue, double saturation, double lightness) {
        if (saturation == 0) {
            var gray = lightness * 2.55;
            return new ModernColor(gray, gray, gray);
        }

        // 色调调整表 - 每30度一个调整值
        var adjustments = new [] {
            +0.1, -0.06, -0.3, // 0°, 30°, 60°
            -0.19, -0.15, -0.24, // 90°, 120°, 150°
            -0.32, -0.09, +0.18, // 180°, 210°, 240°
            +0.05, -0.12, -0.02, // 270°, 300°, 330°
            +0.1, -0.06 // 循环回到开始
        };

        // 计算调整后的中心亮度
        var normalizedHue = ((hue % 360) + 360) % 360;
        var segment = normalizedHue / 30.0;
        var segmentIndex = (int)Math.Floor(segment);
        var interpolation = segment - segmentIndex;

        var adjustment = adjustments[segmentIndex] +
                         interpolation * (adjustments[segmentIndex + 1] - adjustments[segmentIndex]);

        var centerLightness = 50 - adjustment * saturation;

        var adjustedLightness = lightness < centerLightness
            ? lightness / centerLightness * 50
            : 50 + (lightness - centerLightness) / (100 - centerLightness) * 50;

        return FromHsl(hue, saturation, adjustedLightness);
    }

    /// <summary>
    /// HSL转RGB的色调计算
    /// </summary>
    private static double _HueToRgb(double v1, double v2, double vH) {
        vH = ((vH % 1) + 1) % 1; // 确保在0-1范围内

        return vH switch {
            < 1.0 / 6.0 => v1 + (v2 - v1) * 6 * vH,
            < 0.5 => v2,
            < 2.0 / 3.0 => v1 + (v2 - v1) * (2.0 / 3.0 - vH) * 6,
            _ => v1
        };
    }
}

/// <summary>
/// 颜色工具类 - 提供常用的颜色操作
/// </summary>
public static class ColorUtils {
    /// <summary>
    /// 在两个颜色之间进行线性插值
    /// </summary>
    public static ModernColor Lerp(ModernColor from, ModernColor to, double t) =>
        from.Lerp(to, t);

    /// <summary>
    /// 创建颜色渐变序列
    /// </summary>
    public static ModernColor[] CreateGradient(ModernColor from, ModernColor to, int steps) {
        if (steps < 2) throw new ArgumentException("步数至少为2", nameof(steps));

        var colors = new ModernColor[steps];
        for (var i = 0; i < steps; i++) {
            var t = i / (double)(steps - 1);
            colors[i] = Lerp(from, to, t);
        }
        return colors;
    }

    /// <summary>
    /// 混合多个颜色
    /// </summary>
    public static ModernColor BlendColors(params (ModernColor color, double weight)[] colors) {
        if (colors.Length == 0) return ModernColor.Transparent;

        double totalWeight = 0;
        double a = 0, r = 0, g = 0, b = 0;

        foreach (var (color, weight) in colors) {
            if (weight <= 0) continue;

            totalWeight += weight;
            a += color.A * weight;
            r += color.R * weight;
            g += color.G * weight;
            b += color.B * weight;
        }

        return totalWeight == 0 ? ModernColor.Transparent : new ModernColor(a / totalWeight, r / totalWeight, g / totalWeight, b / totalWeight);
    }
}

/// <summary>
/// 扩展方法
/// </summary>
public static class ColorExtensions {
    /// <summary>
    /// 为WPF Color添加现代化操作
    /// </summary>
    public static ModernColor ToModern(this Color color) => new(color);

    /// <summary>
    /// 为字符串添加颜色解析
    /// </summary>
    public static ModernColor AsColor(this string hexString) => new(hexString);
}
