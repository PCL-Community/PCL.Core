using System;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 用于在不便使用泛型的情况下引用 SetupEntry
/// </summary>
public interface ISetupEntry
{
    object Get(string? mcPath = null);
    void Set(object value, string? mcPath = null);
    void Reset(string? mcPath = null);
    bool IsUnset(string? mcPath = null);
    void RaiseChangedEvent(string? mcPath = null);
}

public sealed partial class SetupEntry<T> : ISetupEntry
{
    object ISetupEntry.Get(string? mcPath)
    {
        return Get(mcPath);
    }

    void ISetupEntry.Set(object value, string? mcPath)
    {
        T typed;
        try
        {
            typed = (T)value;
        }
        catch
        {
            throw new ArgumentException($"类型需为 {typeof(T)}", nameof(value));
        }
        Set(typed, mcPath);
    }
}