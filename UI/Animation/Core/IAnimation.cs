using System.Threading.Tasks;

namespace PCL.Core.UI.Animation.Core;

public interface IAnimation
{
    object? CurrentValue { get; internal set; }
    bool IsCompleted { get; }
    int CurrentFrame { get; set; }
    int TotalFrames { get; }
    Task RunAsync();
    void Cancel();
    IAnimationFrame ComputeNextFrame();
}