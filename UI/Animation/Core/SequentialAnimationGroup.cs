using System.Linq;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

/// <summary>
/// 按顺序执行的动画集合。
/// </summary>
public sealed class SequentialAnimationGroup : AnimationGroup
{
    public override async Task<IAnimation> RunAsync(IAnimatable target)
    {
        AnimationService.PushAnimationFireAndForget(this, target);

        ChildrenCore.Clear();
        
        var runtimeChildren = Children
            .Select(child =>
            {
                var childTarget = ResolveTarget(child, target);
                return child.RunAsync(childTarget);
            })
            .ToList();
        
        foreach (var task in runtimeChildren)
        {
            var runChild = await task;
            ChildrenCore.Add(runChild);
        }

        return this;
    }

    public override IAnimation RunFireAndForget(IAnimatable target)
    {
        AnimationService.PushAnimationFireAndForget(this, target);
        
        // 由于顺序执行的特性，这里直接调用异步方法并且不等待其完成，无法享受 FireAndForget 的好处。
        _ = RunAsync(target);

        return this;
    }
}