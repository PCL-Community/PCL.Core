using System;

namespace PCL.Core.Utils;

/// <summary>
/// 用于使用 Using 的语法糖
/// </summary>
public class CallbackDisposable(Action callback) : IDisposable
{
    public void Dispose() => callback.Invoke();
}