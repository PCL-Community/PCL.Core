using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace PCL.Core.Utils;

public static partial class UiHelper {
    // MONITOR_DPI_TYPE enum
    private enum MONITOR_DPI_TYPE {
        MDT_EFFECTIVE_DPI = 0,
        MDT_ANGULAR_DPI = 1,
        MDT_RAW_DPI = 2,
        MDT_DEFAULT = MDT_EFFECTIVE_DPI
    }

    // HMONITOR is IntPtr
    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    // GetDpiForMonitor is in shcore.dll, not user32.dll
    [LibraryImport("shcore.dll", EntryPoint = "GetDpiForMonitor")]
    private static partial int GetDpiForMonitor(
        IntPtr hmonitor,
        MONITOR_DPI_TYPE dpiType,
        out uint dpiX,
        out uint dpiY
    );

    // Get the primary monitor handle
    private const int MONITOR_DEFAULTTOPRIMARY = 1;

    /// <summary>
    /// 获取系统 DPI。
    /// </summary>
    public static int GetSystemDpi() {
        // Get the primary monitor handle
        var hMonitor = MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
        // 0 is S_OK
        var hr = GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _);
        if (hr == 0)
            return (int)dpiX;
        // fallback to default DPI (96)
        return 96;
    }

    /// <summary>
    /// 根据控件当前的 <see cref="FrameworkElement.Margin"/> 以及
    /// <see cref="FrameworkElement.HorizontalAlignment"/> /
    /// <see cref="FrameworkElement.VerticalAlignment"/>，
    /// 计算一个应用了相对位移的新的 <see cref="Thickness"/>。
    /// </summary>
    /// <param name="control">
    /// 需要参与对齐计算的控件。
    /// </param>
    /// <param name="x">
    /// 水平方向的相对位移量。
    /// 正值表示向右的视觉偏移，具体作用边距取决于
    /// <see cref="FrameworkElement.HorizontalAlignment"/>。
    /// </param>
    /// <param name="y">
    /// 垂直方向的相对位移量。
    /// 正值表示向下的视觉偏移，具体作用边距取决于
    /// <see cref="FrameworkElement.VerticalAlignment"/>。
    /// </param>
    /// <returns>
    /// 一个新的具有位移的 <see cref="Thickness"/>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="control"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 当 <paramref name="x"/> 或 <paramref name="y"/> 为
    /// <see cref="double.NaN"/> 或无穷大时抛出。
    /// </exception>
    public static Thickness ComputeAlignedMarginOffset(FrameworkElement control, double x = 0, double y = 0)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (double.IsNaN(x) || double.IsInfinity(x))
            throw new ArgumentOutOfRangeException(nameof(x));

        if (double.IsNaN(y) || double.IsInfinity(y))
            throw new ArgumentOutOfRangeException(nameof(y));

        // Window 不走 Margin 逻辑
        if (control is Window)
            return control.Margin;

        var margin = control.Margin;

        var left = margin.Left;
        var top = margin.Top;
        var right = margin.Right;
        var bottom = margin.Bottom;

        // Horizontal (X)
        switch (control.HorizontalAlignment)
        {
            case HorizontalAlignment.Left:
            case HorizontalAlignment.Stretch:
                left += x;
                break;
            case HorizontalAlignment.Right:
                right -= x;
                break;
            case HorizontalAlignment.Center:
            default:
                // 什么都不做
                break;
        }

        // Vertical (Y)
        switch (control.VerticalAlignment)
        {
            case VerticalAlignment.Top:
            case VerticalAlignment.Stretch:
                top += y;
                break;
            case VerticalAlignment.Bottom:
                bottom -= y;
                break;
            case VerticalAlignment.Center:
            default:
                // 什么都不做
                break;
        }

        return new Thickness(left, top, right, bottom);
    }
}
