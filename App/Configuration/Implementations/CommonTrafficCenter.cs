namespace PCL.Core.App.Configuration.Implementations;

public abstract class CommonTrafficCenter
{
    /// <summary>
    /// 请求操作时触发的事件。
    /// </summary>
    public event TrafficEventHandler? Request;

    /// <summary>
    /// 预览请求操作时触发的事件。
    /// </summary>
    public event PreviewTrafficEventHandler? PreviewRequest;

    /// <summary>
    /// 请求操作实现。
    /// </summary>
    protected abstract void OnRequest<TInput, TOutput>(PreviewTrafficEventArgs<TInput, TOutput> e);

    /// <summary>
    /// 请求操作。
    /// </summary>
    public bool SendRequest<TInput, TOutput>(object? context, TrafficAccess access,
        bool hasInput, TInput? input, bool hasInitialOutput, ref TOutput? output)
    {
        // 初始化事件参数
        var e = hasInput
            ? new PreviewTrafficEventArgs<TInput, TOutput>(input!) { Context = context, Access = access }
            : new PreviewTrafficEventArgs<TInput, TOutput> { Context = context, Access = access };
        if (hasInitialOutput) e.SetOutput(output);
        // 调用实现
        OnRequest(e);
        PreviewRequest?.Invoke(e);
        Request?.Invoke(e);
        if (e.HasOutput) output = e.Output;
        return e.HasOutput;
    }
}
