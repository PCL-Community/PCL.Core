using System;

namespace PCL.Core.ProgramSetup.FileManager;

public interface ISetupFileManager : IDisposable
{
    /// <summary>
    /// 获取某个键对应的值，键不存在时返回 <see langword="null"/>
    /// </summary>
    string? Get(string key, string? mcPath);

    /// <summary>
    /// 设置某个键对应的值，返回旧值（如果之前不存在该键则返回 <see langword="null"/>）
    /// </summary>
    string? Set(string key, string value, string? mcPath);

    /// <summary>
    /// 删除某个键，返回该键的值（如果之前不存在该键则返回 <see langword="null"/>）
    /// </summary>
    string? Remove(string key, string? mcPath);
}

public sealed class MultipleOperationHandle(Action endCallback) : IDisposable
{
    void IDisposable.Dispose() => EndMultipleOperation();
    public void EndMultipleOperation() => endCallback.Invoke();
}