using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;
public class PipelineTask<TLastResult> : TaskBase<TLastResult>
{
    public PipelineTask(string name, Delegate[] delegates, CancellationToken? cancellationToken = null, string? description = null) : base(name, cancellationToken, description)
    {
        List<TaskBase<object>> tasks = [];
        var i = 0;
        foreach (var task in delegates)
        {
            tasks.Add(new TaskBase<object>($"{name} - Pipe {i}", task)); 
            i++; 
        }
        _tasks = tasks;
        if (delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
        CancellationToken?.Register(() => { State = TaskState.Canceled; });
    }

    private readonly List<TaskBase<object>> _tasks;

    public override TLastResult Run(params object[] objects)
    {
        State = TaskState.Running;
        try
        {
            object lastResult = new();
            foreach (var task in _tasks)
                task.ProgressChanged += (_, o, n) =>
                    Progress += (n - o) / _tasks.Count;
            for (var i = 0; i < _tasks.Count; i++)
            {
                object[] param = [lastResult];
                if (i == 0)
                    param = objects;
                CancellationToken?.ThrowIfCancellationRequested();
                lastResult = _tasks[i].Run(param);
            }
            State = TaskState.Completed;
            return Result = (TLastResult)lastResult;
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
            foreach (var task in _tasks)
                task.ProgressChanged += (_, o, n) =>
                    Progress += (n - o) / _tasks.Count;
            object lastResult = new();
            for (var i = 0; i < _tasks.Count; i++)
            {
                object[] param = [lastResult];
                CancellationToken?.ThrowIfCancellationRequested();
                if (i == 0)
                    param = objects;
                lastResult = await _tasks[i].RunAsync(param);
            }
            State = TaskState.Completed;
            return (TLastResult)lastResult;
        }
        catch (Exception)
        {
            if (!(CancellationToken?.IsCancellationRequested ?? false))
                State = TaskState.Failed;
            throw;
        }
    }

    public override void RunBackground(params object[] objects)
        => (BackgroundTask = RunAsync(objects)).Start();
}