using System;
using System.Collections.Frozen;

namespace PCL.Core.UI.Animation.Core;

public record AnimationData<T> : IAnimationData
{
    public AnimationInfo<T> Info { get; init; } = null!;
    public FrozenDictionary<int, T> Values { get; init; } = null!;
}