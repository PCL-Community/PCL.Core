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
    /// 请求标准物流操作。
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
    /// 请求无输出值的物流操作。
    /// </summary>
    public void Request<TInput, TOutput>(object? context, TrafficAccess access, bool hasInput, TInput? input)
    {
        // 初始化事件参数
        var e = _GetEventArgs<TInput, TOutput>(context, access, hasInput, input);
        _OnTraffic(e);
    }

    /// <summary>
    /// 请求有初始输出值且不接收返回值的物流操作。
    /// </summary>
    public void Request<TInput, TOutput>(object? context, TrafficAccess access, bool hasInput, TInput? input, TOutput? initialOutput)
    {
        // 初始化事件参数
        var e = _GetEventArgs<TInput, TOutput>(context, access, hasInput, input);
        e.SetOutput(initialOutput);
        _OnTraffic(e);
    }

    #region IConfigProvider Implementation

    public bool GetValue<T>(string key, [NotNullWhen(true)] out T? value, object? argument)
    {
        T? result = default;
        var hasOutput = Request(argument, TrafficAccess.Read, true, key, false, ref result);
        value = hasOutput ? result : default;
        return hasOutput;
    }

    public void SetValue<T>(string key, T value, object? argument)
    {
        Request(argument, TrafficAccess.Write, true, key, value);
    }

    public void Delete(string key, object? argument)
    {
        Request<string, bool>(argument, TrafficAccess.Write, true, key);
    }

    public bool Exists(string key, object? argument)
    {
        var result = false;
        return Request(argument, TrafficAccess.Read, true, key, true, ref result) && result;
    }

    #endregion
}
