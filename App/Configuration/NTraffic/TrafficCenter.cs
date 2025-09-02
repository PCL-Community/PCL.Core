using System;
using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration.NTraffic;

/// <summary>
/// 物流中心上层实现。
/// </summary>
public abstract class TrafficCenter : ITrafficCenter, IConfigProvider
{
    public event TrafficEventHandler? Traffic;

    public event PreviewTrafficEventHandler? PreviewTraffic;

    /// <summary>
    /// 物流操作实现。
    /// </summary>
    protected abstract void OnTraffic<TInput, TOutput>(
        PreviewTrafficEventArgs<TInput, TOutput> e,
        Action<PreviewTrafficEventArgs<TInput, TOutput>> onInvokeEvent
    );

    private void _OnTraffic<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e)
    {
        OnTraffic(e, ev =>
        {
            PreviewTraffic?.Invoke(ev);
            Traffic?.Invoke(ev);
        });
    }

    private PreviewTrafficEventArgs<TInput, TOutput> _GetEventArgs<TInput, TOutput>(
        object? context, TrafficAccess access, bool hasInput, TInput? input)
    {
        var e = hasInput
            ? new PreviewTrafficEventArgs<TInput, TOutput>(input!) { Context = context, Access = access }
            : new PreviewTrafficEventArgs<TInput, TOutput> { Context = context, Access = access };
        return e;
    }

    /// <summary>
    /// 请求物流操作。
    /// </summary>
    public bool Request<TInput, TOutput>(object? context, TrafficAccess access,
        bool hasInput, TInput? input, bool hasInitialOutput, ref TOutput? output)
    {
        // 初始化事件参数
        var e = _GetEventArgs<TInput, TOutput>(context, access, hasInput, input);
        if (hasInitialOutput) e.SetOutput(output);
        _OnTraffic(e);
        if (e.HasOutput) output = e.Output;
        return e.HasOutput;
    }

    /// <summary>
    /// 请求物流操作。
    /// </summary>
    public bool Request<TInput, TOutput>(object? context, TrafficAccess access, bool hasInput, TInput? input)
    {
        // 初始化事件参数
        var e = _GetEventArgs<TInput, TOutput>(context, access, hasInput, input);
        _OnTraffic(e);
        return e.HasOutput;
    }

    #region IConfigProvider Implementation

    public bool GetValue<T>(string key, [NotNullWhen(true)] out T? value, object? argument = null)
    {
        T? result = default;
        var hasOutput = Request(argument, TrafficAccess.Read, true, key, false, ref result);
        value = hasOutput ? result : default;
        return hasOutput;
    }

    public void SetValue<T>(string key, T value, object? argument = null)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        Request(argument, TrafficAccess.Write, true, key, false, ref value!);
    }

    public void Delete(string key, object? argument = null)
    {
        Request<string, bool>(argument, TrafficAccess.Delete, true, key);
    }

    public bool Exists(string key, object? argument = null)
    {
        var result = false;
        return Request(argument, TrafficAccess.CheckExists, true, key, false, ref result) && result;
    }

    #endregion
}
