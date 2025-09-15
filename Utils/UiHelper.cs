using System;
using System.Runtime.InteropServices;

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
}
