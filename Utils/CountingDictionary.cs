using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PCL.Core.Utils;

/// <summary>
/// 带引用计数的字典，计数归零后释放资源
/// </summary>
public sealed class CountingDictionary<TKey, TValue> : IDisposable
{
    private ConcurrentDictionary<TKey, ValueHolder> _dictionary = new();
    private bool _disposed = false;
    private readonly Func<TKey, TValue>? _valueFactory;
    private readonly Action<TKey, TValue>? _valueDisposer;

    /// <summary>
    /// 创建一个 <see cref="CountingDictionary{TKey,TValue}"/> 实例
    /// </summary>
    /// <param name="valueFactory">默认的值工厂函数</param>
    /// <param name="valueDisposer">默认的值销毁函数</param>
    public CountingDictionary(Func<TKey, TValue>? valueFactory = null, Action<TKey, TValue>? valueDisposer = null)
    {
        _valueFactory = valueFactory;
        _valueDisposer = valueDisposer;
    }

    /// <summary>
    /// 获取一个值，值不存在时会调用工厂创建一个，会将该键的引用 +1
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="valueFactory">可以通过此参数覆盖构造时传入的 valueFactory</param>
    /// <returns>已经有值时返回现有值，否则返回添加的新值</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="valueFactory"/> 和构造时传入的 valueFactory 都为 <see langword="null"/>
    /// </exception>
    /// <exception cref="ObjectDisposedException">该计数字典已被销毁</exception>
    public TValue Acquire(TKey key, Func<TKey, TValue>? valueFactory = null)
    {
        if ((valueFactory ??= _valueFactory) is null)
            throw new ArgumentNullException(nameof(valueFactory));
        if (_disposed)
            throw new ObjectDisposedException("CountingDictionary");

        ValueHolder? newHolder = null;
        while (true)
        {
            // 尝试获取现有值
            if (_dictionary.TryGetValue(key, out var holder))
            {
                lock (holder)
                {
                    // 检查一下该值是否已经没有引用了
                    if (holder.Count > 0)
                    {
                        // 增加引用
                        Interlocked.Increment(ref holder.Count);
                        // 返回值
                        return holder.Value;
                    }
                }
            }
            newHolder ??= new ValueHolder(new Lazy<TValue>(() => valueFactory!.Invoke(key)));
            // 尝试添加新值
            if (_dictionary.TryAdd(key, newHolder))
            {
                return newHolder.Value;
            }
        }
    }

    /// <summary>
    /// 释放引用，会将该键的引用 -1
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="disposer">可以通过此参数覆盖构造时传入的 valueDisposer</param>
    /// <exception cref="ObjectDisposedException">该计数字典已被销毁</exception>
    public void Release(TKey key, Action<TKey, TValue>? disposer = null)
    {
        if (_disposed)
            throw new ObjectDisposedException("CountingDictionary");
        if (!_dictionary.TryGetValue(key, out var holder))
            return;

        lock (holder)
        {
            // 仅在引用被刚好减到 0 时移除并释放，虽然应该也不会有人无缘无故多调几次 方法
            if (Interlocked.Decrement(ref holder.Count) != 0)
                return;
            _dictionary.TryRemove(key, out _);
            (disposer ?? _valueDisposer)?.Invoke(key, holder.Value);
        }
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        // 挨个调用 valueDisposer
        foreach (var pair in _dictionary)
            _valueDisposer?.Invoke(pair.Key, pair.Value.Value);
        // 置空字典
        _dictionary = null!;
    }

    private sealed class ValueHolder(Lazy<TValue> lazyValue)
    {
        public int Count = 1;
        public TValue Value => lazyValue.Value;
    }
}