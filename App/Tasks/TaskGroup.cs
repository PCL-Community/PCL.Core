using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PCL.Core.App.Tasks;

public abstract class TaskGroup : TaskBase, IList<TaskBase>
{
    public TaskGroup(string name, IList<TaskBase> tasks, CancellationToken? cancellationToken = null, string? description = null) : base(name, cancellationToken, description)
    {
        Name = name;
        Tasks = (List<TaskBase>)tasks;
        foreach (TaskBase task in tasks)
            task.RegisterCancellationToken(cancellationToken);
        CancellationToken?.Register(() => { State = TaskState.Canceled; });
    }
    public TaskGroup(string name, IList<Delegate> delegates, CancellationToken? cancellationToken = null, string? description = null) : base(name, cancellationToken, description)
    {
        Name = name;
        List<TaskBase> list = [];
        int i = 0;
        foreach (Delegate @delegate in delegates)
        {
            list.Add(new TaskBase($"{name} - {i}", @delegate, cancellationToken));
            i++;
        }
        Tasks = list;
        CancellationToken?.Register(() => { State = TaskState.Canceled; });
    }

    protected List<TaskBase> Tasks;

    TaskBase IList<TaskBase>.this[int index] { get => Tasks[index]; set => Tasks[index] = value; }

    int ICollection<TaskBase>.Count => Tasks.Count;

    bool ICollection<TaskBase>.IsReadOnly => false;

    void ICollection<TaskBase>.Add(TaskBase item)
        => Tasks.Add(item);

    void ICollection<TaskBase>.Clear()
        => Tasks.Clear();

    bool ICollection<TaskBase>.Contains(TaskBase item)
        => Tasks.Contains(item);

    void ICollection<TaskBase>.CopyTo(TaskBase[] array, int arrayIndex)
        => Tasks.CopyTo(array, arrayIndex);

    IEnumerator<TaskBase> IEnumerable<TaskBase>.GetEnumerator()
        => Tasks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Tasks.GetEnumerator();

    int IList<TaskBase>.IndexOf(TaskBase item)
        => Tasks.IndexOf(item);

    void IList<TaskBase>.Insert(int index, TaskBase item)
        => Tasks.Insert(index, item);

    bool ICollection<TaskBase>.Remove(TaskBase item)
        => Tasks.Remove(item);

    void IList<TaskBase>.RemoveAt(int index)
        => Tasks.RemoveAt(index);
}

public abstract class TaskGroup<TResult> : TaskBase<TResult>, IList<TaskBase>
{
    public TaskGroup(string name, IList<TaskBase> tasks, CancellationToken? cancellationToken = null, string? description = null)
    {
        Name = name;
        Tasks = (List<TaskBase>)tasks;
    }
    public TaskGroup(string name, IList<Delegate> delegates, CancellationToken? cancellationToken = null, string? description = null)
    {
        Name = name;
        List<TaskBase> list = [];
        int i = 0;
        foreach (Delegate @delegate in delegates)
        {
            list.Add(new TaskBase($"{name} - {i}", @delegate));
            i++;
        }
        Tasks = list;
    }

    protected List<TaskBase> Tasks;

    TaskBase IList<TaskBase>.this[int index] { get => Tasks[index]; set => Tasks[index] = value; }

    int ICollection<TaskBase>.Count => Tasks.Count;

    bool ICollection<TaskBase>.IsReadOnly => false;

    void ICollection<TaskBase>.Add(TaskBase item)
        => Tasks.Add(item);

    void ICollection<TaskBase>.Clear()
        => Tasks.Clear();

    bool ICollection<TaskBase>.Contains(TaskBase item)
        => Tasks.Contains(item);

    void ICollection<TaskBase>.CopyTo(TaskBase[] array, int arrayIndex)
        => Tasks.CopyTo(array, arrayIndex);

    IEnumerator<TaskBase> IEnumerable<TaskBase>.GetEnumerator()
        => Tasks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Tasks.GetEnumerator();

    int IList<TaskBase>.IndexOf(TaskBase item)
        => Tasks.IndexOf(item);

    void IList<TaskBase>.Insert(int index, TaskBase item)
        => Tasks.Insert(index, item);

    bool ICollection<TaskBase>.Remove(TaskBase item)
        => Tasks.Remove(item);

    void IList<TaskBase>.RemoveAt(int index)
        => Tasks.RemoveAt(index);
}