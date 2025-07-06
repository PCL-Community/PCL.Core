using System;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 用于在不便使用泛型的情况下引用 SetupEntry
/// </summary>
public interface ISetupEntry
{
    object DefaultValue { get; }
    event Action<object> ValueChanged;
    object Get(string? mcPath = null);
    void Set(object value, string? mcPath = null);
    void Reset(string? mcPath = null);
    bool IsUnset(string? mcPath = null);
    void RaiseChangedEvent(string? mcPath = null);
}

public sealed partial class SetupEntry<T> : ISetupEntry
{
    object ISetupEntry.DefaultValue => DefaultValue;

    event Action<object> ISetupEntry.ValueChanged
    {
        add
        {
            ValueChanged += (mcPath, oldValue, newValue) => value.Invoke(newValue is null ? DefaultValue : newValue.Item1);
        }
        remove
        {
            throw new NotSupportedException();
        }
    }

    object ISetupEntry.Get(string? mcPath)
    {
        return Get(mcPath);
    }

    void ISetupEntry.Set(object value, string? mcPath)
    {
        T typed;
        try
        {
            typed = (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            throw new ArgumentException($"类型需为 {typeof(T)}", nameof(value));
        }
        Set(typed, mcPath);
    }
}