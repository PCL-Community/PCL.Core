using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;

public struct VoidResult;

public class TaskBase<TResult> : IObservableTaskStateSource, IObservableProgressSource
{
    public TaskBase() 
    { 
        Name = ""; 
        _delegate = () => { };
    }
    protected TaskBase(string name, CancellationToken? cancellationToken = null, string? description = null)
    {
        Name = name;
        Description = description;
        CancellationToken = cancellationToken;
        _delegate = () => { };
    }
    public TaskBase(string name, Delegate loadDelegate, CancellationToken? cancellationToken = null, string? description = null)
    {
        Name = name;
        _delegate = loadDelegate;
        CancellationToken = cancellationToken;
        Description = description;
        CancellationToken?.Register(() => { State = TaskState.Canceled; });
    }

    event StateChangedHandler<double>? IStateChangedSource<double>.StateChanged
    {
        add => ProgressChanged += value;
        remove => ProgressChanged -= value;
    }
    event StateChangedHandler<TaskState>? IStateChangedSource<TaskState>.StateChanged
    {
        add => StateChanged += value;
        remove => StateChanged -= value;
    }

    public event StateChangedHandler<double>? ProgressChanged;
    public event StateChangedHandler<TaskState>? StateChanged;

    private double _progress = 0;
    public double Progress {
        get => _progress; 
        set 
        {
            ProgressChanged?.Invoke(this, _progress, value);
            _progress = value;
        } 
    }

    public string Name { get; }
    public string? Description { get; }

    private TaskState _state = TaskState.Waiting;
    public TaskState State
    {
        get => _state;
        set
        {
            StateChanged?.Invoke(this, _state, value);
            _state = value;
        }
    }

    private TResult? _result;
    public TResult? Result
    {
        get
        {
            if (BackgroundTask?.IsCompleted ?? false)
                return BackgroundTask.Result;
            return _result;
        }
        set => _result = value;
    }

    private readonly Delegate _delegate;
    protected readonly CancellationToken? CancellationToken;

    public virtual TResult Run(params object[] objects)
    {
        if (State != TaskState.Waiting)
            throw new Exception($"[TaskBase - {Name}] 运行失败：任务已执行");
        State = TaskState.Running;
        try
        {
            var res = (TResult)(_delegate.DynamicInvoke([this, ..objects]) ?? new object());
            CancellationToken?.ThrowIfCancellationRequested();
            State = TaskState.Completed;
            return _result = res;
        }
        catch (Exception)
        {
            if (!(CancellationToken?.IsCancellationRequested ?? false))
                State = TaskState.Failed;
            throw;
        }
    }

    public virtual async Task<TResult> RunAsync(params object[] objects)
    {
        if (CancellationToken != null)
            return _result = await Task.Run(() => Run(objects), cancellationToken: (CancellationToken)CancellationToken);
        return _result = await Task.Run(() => Run(objects));
    }

    protected Task<TResult>? BackgroundTask;

    public virtual void RunBackground(params object[] objects)
        => (BackgroundTask = RunAsync(objects)).Start();
}