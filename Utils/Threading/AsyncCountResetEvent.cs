using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

// 使用 AI 生成的代码
// 时间: 2025/9/1
// 模型: GPT-5

/// <summary>
/// 一个带配额的 AsyncAutoResetEvent。
/// </summary>
public class AsyncCountResetEvent
{
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private int _permits; // 当前剩余的"配额"
    private readonly object _lock = new();
    
    public Task WaitAsync()
    {
        lock (_lock)
        {
            if (_permits > 0)
            {
                _permits--;
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }

    public void Set(int count = 1)
    {
        lock (_lock)
        {
            while (count > 0 && _waiters.Count > 0)
            {
                var tcs = _waiters.Dequeue();
                tcs.TrySetResult(true);
                count--;
            }

            // 如果没有等待者，就存到配额里
            _permits += count;
        }
    }
}