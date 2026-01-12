using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation.Core;

public class AnimationMixer
{
    private class MixableAnimationFrame(IAnimatable target, object startValue, object value) : IAnimationFrame
    {
        public IAnimatable Target { get; private set; } = target;
        public object StartValue { get; private set; } = startValue;
        public object Value { get; private set; } = value;

        public object GetAbsoluteValue()
        {
            return ValueProcessorManager.Add(StartValue, Value);
        }
    }

    private readonly ConcurrentDictionary<IAnimatable, ImmutableArray<IAnimationFrame>> _cache = new();
    private TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Add(IAnimationFrame frame)
    {
        _cache.AddOrUpdate(frame.Target, 
            [frame],
            (_, list) => list.Add(frame));
        _tcs.TrySetResult();
    }

    public async IAsyncEnumerable<IAnimationFrame> ApplyAsync()
    {
        while (_cache.IsEmpty)
        {
            await _tcs.Task;
        }
        
        foreach (var frames in _cache.Values)
        {
            if (frames.Length == 1)
            {
                yield return frames[0];
                continue;
            }

            var currentValue = frames[0].Value;
            var currentStartValue = frames[0].StartValue;
            
            for (var i = 1; i < frames.Length; i++)
            {
                currentValue = ValueProcessorManager.Add(currentValue, frames[i].Value);
                currentStartValue = ValueProcessorManager.Add(currentStartValue, frames[i].StartValue);
            }

            currentStartValue = ValueProcessorManager.Scale(currentStartValue, 1.0 / frames.Length);
            
            yield return new MixableAnimationFrame(frames[0].Target, currentStartValue, currentValue);
        }
        
        _cache.Clear();
        
        Interlocked.Exchange(
            ref _tcs,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}