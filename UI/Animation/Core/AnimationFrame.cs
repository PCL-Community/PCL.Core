using System.Numerics;
using PCL.Core.UI.Animation.Animatable;

namespace PCL.Core.UI.Animation.Core;

public readonly struct AnimationFrame<T> : IAnimationFrame where T : IAdditionOperators<T, T, T>
{
    public IAnimatable Target { get; init; }
    public T Value { get; init; }
    public T StartValue { get; init; }
    public T GetAbsoluteValue() => StartValue + Value;
    object IAnimationFrame.GetAbsoluteValue() => GetAbsoluteValue();
}