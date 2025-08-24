using Microsoft.VisualBasic;
using PCL.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PCL.Core.UI;

/// <summary>
/// 支持小数与常见类型隐式转换的颜色。
/// </summary>
public class MyColor
{

    public double A = 255d;
    public double R = 0d;
    public double G = 0d;
    public double B = 0d;

    // 类型转换
    public static implicit operator MyColor(string str)
        => new(str);
    public static implicit operator MyColor(Color col)
        => new(col);
    public static implicit operator Color(MyColor conv)
        => Color.FromArgb(MathUtils.Byte(conv.A), MathUtils.Byte(conv.R), MathUtils.Byte(conv.G), MathUtils.Byte(conv.B));
    public static implicit operator System.Drawing.Color(MyColor conv)
        => System.Drawing.Color.FromArgb(MathUtils.Byte(conv.A), MathUtils.Byte(conv.R), MathUtils.Byte(conv.G), MathUtils.Byte(conv.B));
    public static implicit operator MyColor(SolidColorBrush bru)
        => new(bru.Color);
    public static implicit operator SolidColorBrush(MyColor conv)
        => new SolidColorBrush(Color.FromArgb(MathUtils.Byte(conv.A), MathUtils.Byte(conv.R), MathUtils.Byte(conv.G), MathUtils.Byte(conv.B)));
    public static implicit operator MyColor(Brush bru)
        => new(bru);
    public static implicit operator Brush(MyColor conv)
        => new SolidColorBrush(Color.FromArgb(MathUtils.Byte(conv.A), MathUtils.Byte(conv.R), MathUtils.Byte(conv.G), MathUtils.Byte(conv.B)));

    // 颜色运算
    public static MyColor operator +(MyColor a, MyColor b)
        => new() { A = a.A + b.A, B = a.B + b.B, G = a.G + b.G, R = a.R + b.R };
    public static MyColor operator -(MyColor a, MyColor b)
        => new() { A = a.A - b.A, B = a.B - b.B, G = a.G - b.G, R = a.R - b.R };
    public static MyColor operator *(MyColor a, double b)
        => new() { A = a.A * b, B = a.B * b, G = a.G * b, R = a.R * b };
    public static MyColor operator /(MyColor a, double b)
        => new() { A = a.A / b, B = a.B / b, G = a.G / b, R = a.R / b };
    public static bool operator ==(MyColor? a, MyColor? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        return a.A == b.A && a.R == b.R && a.G == b.G && a.B == b.B;
    }
    public static bool operator !=(MyColor? a, MyColor? b)
    {
        if (a == null && b == null)
            return false;
        if (a == null || b == null)
            return true;
        return !(a.A == b.A && a.R == b.R && a.G == b.G && a.B == b.B);
    }

    // 构造函数
    public MyColor()
    {
    }
    public MyColor(Color col)
    {
        A = col.A;
        R = col.R;
        G = col.G;
        B = col.B;
    }
    public MyColor(string hexString)
    {
        Color stringColor = (Color)ColorConverter.ConvertFromString(hexString);
        A = stringColor.A;
        R = stringColor.R;
        G = stringColor.G;
        B = stringColor.B;
    }
    public MyColor(double newA, MyColor col)
    {
        A = newA;
        R = col.R;
        G = col.G;
        B = col.B;
    }
    public MyColor(double newR, double newG, double newB)
    {
        A = 255d;
        R = newR;
        G = newG;
        B = newB;
    }
    public MyColor(double newA, double newR, double newG, double newB)
    {
        A = newA;
        R = newR;
        G = newG;
        B = newB;
    }
    public MyColor(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        A = color.A;
        R = color.R;
        G = color.G;
        B = color.B;
    }
    public MyColor(SolidColorBrush brush)
    {
        var color = brush.Color;
        A = color.A;
        R = color.R;
        G = color.G;
        B = color.B;
    }
    public MyColor(object obj)
    {
        if (obj is null)
        {
            A = 255d;
            R = 255d;
            G = 255d;
            B = 255d;
        }
        else if (obj is SolidColorBrush)
        {
            // 避免反复获取 Color 对象造成性能下降
            var color = ((SolidColorBrush)obj).Color;
            A = color.A;
            R = color.R;
            G = color.G;
            B = color.B;
        }
        else
        {
            A = (double)((dynamic)obj).A;
            R = (double)((dynamic)obj).R;
            G = (double)((dynamic)obj).G;
            B = (double)((dynamic)obj).B;
        }
    }

    // HSL
    public double Hue(double v1, double v2, double vH)
    {
        if (vH < 0d)
            vH += 1d;
        if (vH > 1d)
            vH -= 1d;
        if (vH < 0.16667d)
            return v1 + (v2 - v1) * 6d * vH;
        if (vH < 0.5d)
            return v2;
        if (vH < 0.66667d)
            return v1 + (v2 - v1) * (4d - vH * 6d);
        return v1;
    }
    public MyColor FromHSL(double sH, double sS, double sL)
    {
        if (sS == 0d)
        {
            R = sL * 2.55d;
            G = R;
            B = R;
        }
        else
        {
            double h = sH / 360d;
            double s = sS / 100d;
            double l = sL / 100d;
            s = l < 0.5d ? s * l + l : s * (1.0d - l) + l;
            l = 2d * l - s;
            R = 255d * Hue(l, s, h + 1d / 3d);
            G = 255d * Hue(l, s, h);
            B = 255d * Hue(l, s, h - 1d / 3d);
        }
        A = 255d;
        return this;
    }
    public MyColor FromHSL2(double sH, double sS, double sL)
    {
        if (sS == 0d)
        {
            R = sL * 2.55d;
            G = R;
            B = R;
        }
        else
        {
            // 初始化
            sH = (sH + 3600000d) % 360d;
            double[] cent = [+0.1d, -0.06d, -0.3d, -0.19d, -0.15d, -0.24d, -0.32d, -0.09d, +0.18d, +0.05d, -0.12d, -0.02d, +0.1d, -0.06d];  // 0, 30, 60
                                                                                                                                            // 90, 120, 150
                                                                                                                                            // 180, 210, 240
                                                                                                                                            // 270, 300, 330
                                                                                                                                            // 最后两位与前两位一致，加是变亮，减是变暗
                                                                                                                                            // 计算色调对应的亮度片区
            double center = sH / 30.0d;
            int intCenter = (int)Math.Round(Math.Floor(center)); // 亮度片区编号
            center = 50d - ((1d - center + intCenter) * cent[intCenter] + (center - intCenter) * cent[intCenter + 1]) * sS;
            // center = 50 + (cent(intCenter) + (center - intCenter) * (cent(intCenter + 1) - cent(intCenter))) * sS
            sL = (sL < center ? sL / center : 1d + (sL - center) / (100d - center)) * 50d;
            FromHSL(sH, sS, sL);
        }
        A = 255d;
        return this;
    }

    public MyColor Alpha(double sA)
    {
        A = sA;
        return this;
    }

    public override string ToString()
    {
        return "(" + A + "," + R + "," + G + "," + B + ")";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) 
            return false;
        return this == (MyColor)obj;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

