using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Helper
{
    public static class BlurHelper
    {
        public static event EventHandler<int> BlurChanged;

        public static void RaiseBlurChanged(int blurValue)
        {
            BlurChanged?.Invoke(null, blurValue);
        }
    }
}
