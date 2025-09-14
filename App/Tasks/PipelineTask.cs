using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;

/// <summary>
/// 管道任务。<br/>
/// 后一个委托的参数会传入前一个委托的返回值。<br/>
/// </summary>
/// <typeparam name="TLastResult">最终的返回类型</typeparam>
public class PipelineTask<TLastResult> : TaskGroup<TLastResult>
{
    public PipelineTask(string name, IList<TaskBase> taskBases, CancellationToken? cancellationToken = null, string? description = null) : base(name, taskBases, cancellationToken, description)
    {
        if (taskBases.Last().ResultType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
    }
    public PipelineTask(string name, IList<Delegate> delegates, CancellationToken? cancellationToken = null, string? description = null) : base(name, delegates, cancellationToken, description)
    {
        if (delegates.Last().Method.ReturnType != typeof(TLastResult))
            throw new Exception($"[PipelineTask - {name}] 构造失败：不匹配的返回类型");
    }

    public override TLastResult Run(params object[] objects)
    {
        State = TaskState.Running;
        try
        {
            object? lastResult = new();
            foreach (var task in Tasks)
                task.ProgressChanged += (_, o, n) =>
                    Progress += (n - o) / Tasks.Count;
            for (var i = 0; i < Tasks.Count; i++)
            {
                object[] param = [];
                if (lastResult != null)
                    param = [lastResult];
                if (i == 0)
                    param = objects;
                CancellationToken?.ThrowIfCancellationRequested();
                lastResult = Tasks[i].Run(param);
            }
            State = TaskState.Completed;
            if (lastResult == null)
                throw new Exception($"[PipelineTask - {Name}] 最后的结果是空的。");
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
            foreach (var task in Tasks)
                task.ProgressChanged += (_, o, n) =>
                    Progress += (n - o) / Tasks.Count;
            object? lastResult = new();
            for (var i = 0; i < Tasks.Count; i++)
            {
                object[] param = [];
                if (lastResult != null)
                    param = [lastResult];
                CancellationToken?.ThrowIfCancellationRequested();
                if (i == 0)
                    param = objects;
                lastResult = await Tasks[i].RunAsync(param);
            }
            State = TaskState.Completed;
            if (lastResult == null)
                throw new Exception($"[PipelineTask - {Name}] 最后的结果是空的。");
            return Result = (TLastResult)lastResult;
        }
        catch (Exception)
        {
            if (!(CancellationToken?.IsCancellationRequested ?? false))
                State = TaskState.Failed;
            throw;
        }
    }

    public override void RunBackground(params object[] objects)
        => RunAsync(objects).Start();
}