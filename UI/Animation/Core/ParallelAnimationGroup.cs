using System.Linq;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

/// <summary>
/// 同时执行的动画集合。
/// </summary>
public sealed class ParallelAnimationGroup : AnimationGroup
{
    public override Task RunAsync(IAnimatable target)
    {
        // 取出所有子动画的任务，并等待它们全部完成
        var tasks = Children.Select(child =>
        {
            var childTarget = ResolveTarget(child, target);
            return child.RunAsync(childTarget);
        });
        
        return Task.WhenAll(tasks);
    }
}