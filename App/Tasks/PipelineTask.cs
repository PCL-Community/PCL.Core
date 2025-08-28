using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;
public class PipelineTask<TLastResult> : TaskBase<TLastResult>
{
    public PipelineTask(string name, Delegate[] delegates, string? description) : base(name, description)
    {
        _delegates = delegates;
        if (_delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
    }
    public PipelineTask(string name, Delegate[] delegates, CancellationToken? cancellationToken, string? description) : base(name, cancellationToken, description)
    {
        _delegates = delegates;
        if (_delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
        if (CancellationToken != null)
            ((CancellationToken)CancellationToken).Register(() => { State = TaskState.Canceled; });
    }

    private readonly Delegate[] _delegates;

    public override TLastResult Run(params object[] objects)
    {
        State = TaskState.Running;
        try
        {
            object lastResult = new();
            for (int i = 0; i < objects.Length; i++)
            {
                var param = (object[])objects[i];
                CancellationToken?.ThrowIfCancellationRequested();
                lastResult = typeof(Delegate).GetMethod("DynamicInvoke")?.Invoke(_delegates[i], [this, .. param]) ?? new();
            }
            return (TLastResult)lastResult;
        }
        catch (Exception)
        {
            if (!(CancellationToken?.IsCancellationRequested ?? false))
                State = TaskState.Failed;
            throw;
        }
    }

    public override async Task<TLastResult> RunAsync(params object[] objects)
    {
        State = TaskState.Running;
        try
        {
            object lastResult = new();
            if (objects.Length != _delegates.Length)
                throw new Exception($"[PipelineTask - {Name}] 运行失败：委托组长度与参数组长度不匹配");
            async Task<TLastResult> Task()
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    object[] param = [lastResult];
                    if (i == 0)
                        param = objects;
                    if (CancellationToken != null)
                    {
                        ((CancellationToken)CancellationToken).ThrowIfCancellationRequested();
                        lastResult = await new Task<object>(() => typeof(Delegate).GetMethod("DynamicInvoke")?.Invoke(_delegates[i], [this, .. param]) ?? new(), cancellationToken: (CancellationToken)CancellationToken);
                    }
                    else
                        lastResult = await new Task<object>(() => typeof(Delegate).GetMethod("DynamicInvoke")?.Invoke(_delegates[i], [this, .. param]) ?? new());
                }
                State = TaskState.Completed;
                return (TLastResult)lastResult;
            }
            return await Task();
        }
        catch (Exception)
        {
            if (!(CancellationToken?.IsCancellationRequested ?? false))
                State = TaskState.Failed;
            throw;
        }
    }

    public override void RunBackground(params object[] objects)
    {
        try
        {
            (BackgroundTask = (Task<TLastResult>)(typeof(PipelineTask<TLastResult>).GetMethod("RunAsync")?.Invoke(this, objects) ?? new())).Start();
        }
        catch (Exception)
        {
            throw;
        }
    }
}