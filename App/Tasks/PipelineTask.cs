using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;
public class PipelineTask<TLastResult> : TaskBase<TLastResult>
{
    public PipelineTask(string name, Delegate[] delegates, string? description) : base(name, description: description)
    {
        _tasks = [];
        int i = 0;
        foreach (var task in delegates)
        { 
            _ = _tasks.Append(new TaskBase<object>($"{name} - Pipe {i}", task)); 
            i++; 
        }
        if (delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
    }
    public PipelineTask(string name, Delegate[] delegates, CancellationToken? cancellationToken, string? description) : base(name, cancellationToken, description)
    {
        _tasks = [];
        int i = 0;
        foreach (var task in delegates)
        {
            _ = _tasks.Append(new TaskBase<object>($"{name} - Pipe {i}", task)); 
            i++; 
        }
        if (delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
        if (CancellationToken != null)
            ((CancellationToken)CancellationToken).Register(() => { State = TaskState.Canceled; });
    }

    private readonly TaskBase<object>[] _tasks;

    public override TLastResult Run(params object[] objects)
    {
        State = TaskState.Running;
        try
        {
            object lastResult = new();
            if (objects.Length != _tasks.Length)
                throw new Exception($"[PipelineTask - {Name}] 运行失败：委托组长度与参数组长度不匹配");
            foreach (var task in _tasks)
                task.ProgressChanged += (s, o, n) =>
                    Progress += (n - o) / _tasks.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                var param = (object[])objects[i];
                CancellationToken?.ThrowIfCancellationRequested();
                lastResult = typeof(TaskBase<object>).GetMethod("Run")?.Invoke(_tasks[i], [this, .. param]) ?? new();
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
            if (objects.Length != _tasks.Length)
                throw new Exception($"[PipelineTask - {Name}] 运行失败：委托组长度与参数组长度不匹配");
            foreach ( var task in _tasks )
                task.ProgressChanged += (s, o, n) =>
                    Progress += (n - o) / _tasks.Length;
            async Task<TLastResult> Task()
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    object[] param = [lastResult];
                    if (i == 0)
                        param = objects;
                    lastResult = await (Task<object>)(typeof(TaskBase<object>).GetMethod("RunAsync")?.Invoke(_tasks[i], param) ?? new());
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
        (BackgroundTask = (Task<TLastResult>)(typeof(PipelineTask<TLastResult>).GetMethod("RunAsync")?.Invoke(this, objects) ?? new())).Start();
    }
}