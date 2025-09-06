using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Easings;

public class BounceEaseIn : Easing
{
    protected override double EaseCore(double progress)
    {
        return 1 - BounceEaseUtils.Bounce(1 - progress);
    }
}