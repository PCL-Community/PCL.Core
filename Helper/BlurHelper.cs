using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Helper
{
    public static class BlurHelper
    {
        public static event EventHandler<bool> BlurChanged;

        public static void RaiseBlurChanged(bool isBlurred)
        {
            BlurChanged?.Invoke(null, isBlurred);
        }
    }
}
