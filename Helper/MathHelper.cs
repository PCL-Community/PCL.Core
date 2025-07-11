using System;

namespace PCL.Core.Helper
{
    internal static class MathHelper
    {
        internal const double DBL_EPSILON = 2.2204460492503131e-016;

        public static bool IsZero(double value) => Math.Abs(value) < 10.0 * DBL_EPSILON;
    }
}
