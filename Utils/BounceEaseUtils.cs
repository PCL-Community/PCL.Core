namespace PCL.Core.Utils;

internal static class BounceEaseUtils
{
    internal static double Bounce(double progress)
    {
        // 预计算常量
        const double n1 = 7.5625;
        const double invD1 = 0.3636363636363636;      // 1 / 2.75
        const double threshold2 = 0.7272727272727273;  // 2 / 2.75
        const double threshold3 = 0.9090909090909091;  // 2.5 / 2.75
        const double offset1 = 0.5454545454545454;    // 1.5 / 2.75
        const double offset2 = 0.8181818181818182;    // 2.25 / 2.75
        const double offset3 = 0.9545454545454546;    // 2.625 / 2.75

        switch (progress)
        {
            case < invD1:
                return n1 * progress * progress;
            case < threshold2:
                progress -= offset1;
                return n1 * progress * progress + 0.75;
            case < threshold3:
                progress -= offset2;
                return n1 * progress * progress + 0.9375;
            default:
                progress -= offset3;
                return n1 * progress * progress + 0.984375;
        }
    }
}