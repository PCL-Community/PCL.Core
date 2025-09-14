using System;
using System.Runtime.InteropServices;

namespace PCL.Core.Utils;

public static partial class UiHelper {
    [LibraryImport("user32.dll", EntryPoint = "GetDpiForMonitor")]
    private static partial uint GetDpiForMonitor();

    /// <summary>
    /// 获取系统 DPI。
    /// </summary>
    public static int GetSystemDpi() {
        return (int) GetDpiForMonitor();
    }
}
