using PCL.Core.Logging;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PCL.Core.Utils.OS
{
    public partial class WindowManagerInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS { public int leftWidth, rightWidth, topHeight, bottomHeight; }

        [LibraryImport("dwmapi.dll")]
        private static partial int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [LibraryImport("dwmapi.dll")]
        private static partial int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled);

        /// <summary>
        /// DWM 组合是否可用
        /// </summary>
        /// <returns></returns>
        public static bool IsCompositionEnabled()
        {
            int hResult = DwmIsCompositionEnabled(out var enabled);
            if (hResult != 0)
                throw new Win32Exception(hResult, "Failed to check DWM status");
            return enabled;
        }

        public static void ExtendFrameIntoClientArea(IntPtr hWnd, int margin)
        {
            MARGINS margins = new() { leftWidth = margin, rightWidth = margin, topHeight = margin, bottomHeight = margin };
            if (IsCompositionEnabled())
            {
                int hResult = DwmExtendFrameIntoClientArea(hWnd, ref margins);
                if (hResult != 0) throw new Win32Exception(hResult, "Failed to extend frame into client area");
            }
        }
    }
}
