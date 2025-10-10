using System;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public interface IAnimation
{
    bool IsCompleted { get; }
    int CurrentFrame { get; set; }
    Task RunAsync(IAnimatable target);
    void Cancel();
    IAnimationFrame? ComputeNextFrame(IAnimatable target);
    public void RaiseStarted();
    public void RaiseCompleted(); 
    public event EventHandler Started;
    public event EventHandler Completed;
}