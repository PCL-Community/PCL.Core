using System;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.App.Tasks;

public class TaskBase<TResult> : IObservableTaskStateSource, IObservableProgressSource
{
    public TaskBase() 
    { 
        _name = ""; 
        _delegate = () => { };
    }
    protected TaskBase(string name, string? description)
    {
        _name = name;
        _description = description;
        _delegate = () => { };
    }
    protected TaskBase(string name, CancellationToken? cancellationToken, string? description)
    {
        _name = name;
        _description = description;
        CancellationToken = cancellationToken;
        _delegate = () => { };
    }
    public TaskBase(string name, Delegate loadDelegate, string? description)
    {
        _name = name;
        _description = description;
        _delegate = loadDelegate;
    }
    public TaskBase(string name, Delegate loadDelegate, CancellationToken? cancellationToken, string? description)
    {
        _name = name;
        _delegate = loadDelegate;
        CancellationToken = cancellationToken;
        _description = description;
        if (CancellationToken != null)
            ((CancellationToken)CancellationToken).Register(() => { State = TaskState.Canceled; });
    }

    private double _progress = 0;
    public double Progress {
        get => _progress; 
        set 
        {
            ProgressChanged?.Invoke(this, _progress, value);
            _progress = value;
        } 
    }

    private readonly string _name;
    public string Name { get => _name; }

    private readonly string? _description;
    public string? Description {  get => _description; }

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
    }

    public event PropertyChangedHandler<double>? ProgressChanged;
    public event PropertyChangedHandler<TaskState>? StateChanged;

    private readonly Delegate _delegate;
    protected readonly CancellationToken? CancellationToken;

    public virtual TResult Run(params object[] objects)
    {
        if (State != TaskState.Waiting)
            throw new Exception($"[TaskBase - {Name}] 运行失败：任务已执行");
        State = TaskState.Running;
        try
        {
            var res = (TResult)(typeof(Delegate).GetMethod("DynamicInvoke")?.Invoke(_delegate, [this, .. objects]) ?? new());
            if (CancellationToken?.IsCancellationRequested ?? false)
                ((CancellationToken)CancellationToken).ThrowIfCancellationRequested();
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
        try
        {
            if (CancellationToken != null)
                return _result = await new Task<TResult>(() => (TResult)(typeof(TaskBase<TResult>).GetMethod("Run")?.Invoke(this, objects) ?? new()), cancellationToken: (CancellationToken)CancellationToken);
            return _result = await new Task<TResult>(() => (TResult)(typeof(TaskBase<TResult>).GetMethod("Run")?.Invoke(this, objects) ?? new()));
        }
        catch (Exception)
        {
            throw;
        }
    }

    protected Task<TResult>? BackgroundTask;
    public virtual void RunBackground(params object[] objects)
    {
        try
        {
            (BackgroundTask = (Task<TResult>)(typeof(TaskBase<TResult>).GetMethod("RunAsync")?.Invoke(this, objects) ?? new())).Start();
        }
        catch (Exception)
        {
            throw;
        }
    }
}
