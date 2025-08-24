using PCL.Core.UI;
using System;
using System.Linq;

namespace PCL.Core.Utils;

public static class MathUtils
{
    /// <summary>
    /// 将数值限定在某个范围内。
    /// </summary>
    public static double Clamp(double value, double min, double max) 
        => Math.Max(min, Math.Min(max, value));

    /// <summary>
    /// 2~65 进制的转换。
    /// </summary>
    public static string RadixConvert(string input, int fromRadix, int toRadix)
    {
        const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+=";
        // 零与负数的处理
        if (string.IsNullOrEmpty(input))
            return "0";
        bool isNegative = input.StartsWith("-");
        if (isNegative)
            input = input.TrimStart('-');
        // 转换为十进制
        long realNum = 0L;
        long scale = 1L;
        foreach (var digit in input.Reverse().Select(l => digits.IndexOf(l.ToString())))
        {
            realNum += digit * scale;
            scale *= fromRadix;
        }
        // 转换为指定进制
        string result = "";
        while (realNum > 0L)
        {
            int newNum = (int)(realNum % toRadix);
            realNum = (long)Math.Round((realNum - newNum) / (double)toRadix);
            result = digits[newNum] + result;
        }
        // 负数的结束处理与返回
        return (isNegative ? "-" : "") + result;
    }

    /// <summary>
    /// 计算二阶贝塞尔曲线。
    /// </summary>
    public static double Bezier(double x, double x1, double y1, double x2, double y2, double acc = 0.01d)
    {
        if (x <= 0d || double.IsNaN(x))
            return 0d;
        if (x >= 1d)
            return 1d;
        double a, b;
        a = x;
        do
        {
            b = 3 * a * ((0.33333333 + x1 - x2) * a * a + (x2 - 2 * x1) * a + x1);
            a += (x - b) * 0.5;
        }
        while (Math.Abs(b - x) < acc); // 精度
        return 3 * a * ((0.33333333 + y1 - y2) * a * a + (y2 - 2 * y1) * a + y1);
    }

    /// <summary>
    /// 提供 MyColor 类型支持的 Math.Round。
    /// </summary>
    public static MyColor Round(MyColor col, int w = 0)
    {
        return new MyColor() { A = Math.Round(col.A, w), R = Math.Round(col.R, w), G = Math.Round(col.G, w), B = Math.Round(col.B, w) };
    }

    /// <summary>
    /// 将一个数字限制为 0~255 的 Byte 值。
    /// </summary>
    public static byte Byte(double d)
    {
        if (d < 0d)
            d = 0d;
        if (d > 255d)
            d = 255d;
        return (byte)Math.Round(Math.Round(d));
    }

    /// <summary>
    /// 获取两数间的百分比。小数点精确到 6 位。
    /// </summary>
    /// <returns></returns>
    public static double Percent(double a, double b, double percent)
    {
        return Math.Round(a * (1d - percent) + b * percent, 6); // 解决 Double 计算错误
    }

    /// <summary>
    /// 获取两颜色间的百分比，根据 RGB 计算。小数点精确到 6 位。
    /// </summary>
    public static MyColor Percent(MyColor a, MyColor b, double percent)
    {
        return Round(a * (1d - percent) + b * percent, 6); // 解决Double计算错误
    }

    /// <summary>
    /// 符号函数。
    /// </summary>
    public static int Sgn(double value)
    {
        if (value == 0d)
            return 0;
        else if (value > 0d)
            return 1;
        else
            return -1;
    }
}

