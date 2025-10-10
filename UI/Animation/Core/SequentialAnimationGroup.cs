using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

/// <summary>
/// 按顺序执行的动画集合。
/// </summary>
public sealed class SequentialAnimationGroup : AnimationGroup
{
    public override async Task RunAsync(IAnimatable target)
    {
        foreach (var child in Children)
        {
            var childTarget = ResolveTarget(child, target);
            await child.RunAsync(childTarget);
        }
    }
}