using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Utils.OS
{
    public partial class WindowManagerInterop
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS { public int leftWidth, rightWidth, topHeight, bottomHeight; }

        [LibraryImport("dwmapi.dll")]
        public static partial int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
    }
}
