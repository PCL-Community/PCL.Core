using System;
using System.Numerics;
using System.Windows.Media;

namespace PCL.Core.UI;

public struct NColor
{
    private Vector4 _color;

    public float R
    {
        get => _color.X;
        set => _color.X = value;
    }
    public float G
    {
        get => _color.Y;
        set => _color.Y = value;
    }
    public float B
    {
        get => _color.Z;
        set => _color.Z = value;
    }
    public float A
    {
        get => _color.W;
        set => _color.W = value;
    }

    #region 构造函数
    
    public NColor()
    {
        _color = new Vector4(0f, 0f, 0f, 255f);
    }

    public NColor(float r, float g, float b, float a = 255f)
    {
        _color = new Vector4(r, g, b, a);
    }
    
    public NColor(Color color) : this(color.R, color.G, color.B, a: color.A) { }
    
    public NColor(System.Drawing.Color color) : this(color.R, color.G, color.B, color.A) { }

    public NColor(string hex) : this((Color)ColorConverter.ConvertFromString(hex)) { }

    public NColor(float a, NColor color) : this(color.R, color.G, color.B, a) { }

    public NColor(float r, float g, float b) : this(r, g, b, 255f) { }

    public NColor(SolidColorBrush brush) : this(brush.Color) { }

    public NColor(Brush brush) : this((SolidColorBrush)brush) { }
    
    #endregion

    #region 运算符重载
    
    public static NColor operator +(NColor a, NColor b)
    {
        return new NColor(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
    }

    public static NColor operator -(NColor a, NColor b)
    {
        return new NColor(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);
    }

    public static NColor operator *(NColor a, float b)
    {
        return new NColor(a.R * b, a.G * b, a.B * b, a.A * b);
    }

    public static NColor operator /(NColor a, float b)
    {
        if (b == 0) throw new DivideByZeroException("除数不能为零。");
        return new NColor(a.R / b, a.G / b, a.B / b, a.A / b);
    }

    public static bool operator ==(NColor a, NColor b)
    {
        return a._color == b._color;
    }

    public static bool operator !=(NColor a, NColor b)
    {
        return a._color != b._color;
    }

    #endregion

    #region IEquatable
    
    public bool Equals(NColor other)
    {
        return _color.Equals(other._color);
    }

    public override bool Equals(object? obj)
    {
        if (obj is NColor color)
            return Equals(color);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    #endregion

    #region HSL 
    
    public static NColor FromHsl(double sH, double sS, double sL)
    {
        var color = new NColor();
        if (sS == 0)
        {
            color.R = (float)(sL * 2.55);
            color.G = color.R;
            color.B = color.R;
        }
        else
        {
            var h = sH / 360;
            var s = sS / 100;
            var l = sL / 100;
            s = l < 0.5 ? s * l + l : s * (1.0 - l) + l;
            l = 2 * l - s;
            color.R = (float)(255 * _Hue(l, s, h + 1 / 3.0));
            color.G = (float)(255 * _Hue(l, s, h));
            color.B = (float)(255 * _Hue(l, s, h - 1 / 3.0));
        }

        color.A = 255;
        return color;
    }

    private static double _Hue(double v1, double v2, double vH)
    {
        if (vH < 0) vH += 1;
        if (vH > 1) vH -= 1;
        if (vH < 0.16667) return v1 + (v2 - v1) * 6 * vH;
        if (vH < 0.5) return v2;
        if (vH < 0.66667) return v1 + (v2 - v1) * (4 - vH * 6);
        return v1;
    }
    
    #endregion
}
