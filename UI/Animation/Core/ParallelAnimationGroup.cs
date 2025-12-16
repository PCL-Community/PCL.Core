using System.Linq;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

/// <summary>
/// 同时执行的动画集合。
/// </summary>
public sealed class ParallelAnimationGroup : AnimationGroup
{
    public override async Task<IAnimation> RunAsync(IAnimatable target)
    {
        AnimationService.PushAnimationFireAndForget(this, target);

        ChildrenCore.Clear();

        var tasks = Children.Select(async child =>
        {
            var childTarget = ResolveTarget(child, target);
            
            var runChild = await child.RunAsync(childTarget);

            lock (ChildrenCore)
            {
                ChildrenCore.Add(runChild);
            }
        });

        await Task.WhenAll(tasks);
        return this;
    }

    public override IAnimation RunFireAndForget(IAnimatable target)
    {
        AnimationService.PushAnimationFireAndForget(this, target);

        ChildrenCore.Clear();

        foreach (var child in Children)
        {
            var childTarget = ResolveTarget(child, target);
            var runChild = child.RunFireAndForget(childTarget);
            ChildrenCore.Add(runChild);
        }

        return this;
    }
}