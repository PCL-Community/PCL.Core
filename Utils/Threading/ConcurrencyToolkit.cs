using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Threading;

/// <summary>
/// 高性能并发工具包
/// 使用无锁设计、Channel、ValueTask等技术
/// </summary>
public static class ConcurrencyToolkit
{
    /// <summary>
    /// 双线程池 - 使用Channel和无锁设计
    /// </summary>
    public class DualThreadPool : IDisposable
    {
        private readonly Channel<Func<ValueTask>> _ioChannel;
        private readonly Channel<Func<ValueTask>> _cpuChannel;
        private readonly ChannelWriter<Func<ValueTask>> _ioWriter;
        private readonly ChannelWriter<Func<ValueTask>> _cpuWriter;
        private readonly Task[] _ioWorkers;
        private readonly Task[] _cpuWorkers;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        // 性能统计
        private long _ioTasksCompleted;
        private long _cpuTasksCompleted;
        private long _totalIoTime;
        private long _totalCpuTime;
        
        /// <summary>
        /// IO任务完成数量
        /// </summary>
        public long IoTasksCompleted => _ioTasksCompleted;
        
        /// <summary>
        /// CPU任务完成数量  
        /// </summary>
        public long CpuTasksCompleted => _cpuTasksCompleted;
        
        /// <summary>
        /// 平均IO任务执行时间(微秒)
        /// </summary>
        public double AverageIoTime => _ioTasksCompleted > 0 ? (double)_totalIoTime / _ioTasksCompleted / 10 : 0;
        
        /// <summary>
        /// 平均CPU任务执行时间(微秒)
        /// </summary>
        public double AverageCpuTime => _cpuTasksCompleted > 0 ? (double)_totalCpuTime / _cpuTasksCompleted / 10 : 0;
        
        public DualThreadPool(int ioThreads = -1, int cpuThreads = -1)
        {
            // 智能线程数量计算
            ioThreads = ioThreads <= 0 ? Environment.ProcessorCount * 2 : ioThreads;
            cpuThreads = cpuThreads <= 0 ? Environment.ProcessorCount : cpuThreads;
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 创建高性能有界Channel
            var channelOptions = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };
            
            _ioChannel = Channel.CreateBounded<Func<ValueTask>>(channelOptions);
            _cpuChannel = Channel.CreateBounded<Func<ValueTask>>(channelOptions);
            _ioWriter = _ioChannel.Writer;
            _cpuWriter = _cpuChannel.Writer;
            
            // 启动工作线程
            _ioWorkers = new Task[ioThreads];
            _cpuWorkers = new Task[cpuThreads];
            
            for (int i = 0; i < ioThreads; i++)
                _ioWorkers[i] = CreateWorkerTask(_ioChannel.Reader, true, $"IO-{i}");
                
            for (int i = 0; i < cpuThreads; i++)
                _cpuWorkers[i] = CreateWorkerTask(_cpuChannel.Reader, false, $"CPU-{i}");
        }
        
        /// <summary>
        /// 创建工作任务
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private Task CreateWorkerTask(ChannelReader<Func<ValueTask>> reader, bool isIo, string threadName)
        {
            return Task.Run(async () =>
            {
                Thread.CurrentThread.Name = threadName;
                var stopwatch = Stopwatch.StartNew();
                
                try
                {
                    await foreach (var work in reader.ReadAllAsync(_cancellationTokenSource.Token))
                    {
                        stopwatch.Restart();
                        try
                        {
                            await work().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // 记录异常但不中断工作线程
                            Debug.WriteLine($"Worker {threadName} exception: {ex}");
                        }
                        
                        // 更新统计信息
                        var elapsed = stopwatch.ElapsedTicks;
                        if (isIo)
                        {
                            Interlocked.Increment(ref _ioTasksCompleted);
                            Interlocked.Add(ref _totalIoTime, elapsed);
                        }
                        else
                        {
                            Interlocked.Increment(ref _cpuTasksCompleted);
                            Interlocked.Add(ref _totalCpuTime, elapsed);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                }
            });
        }
        
        /// <summary>
        /// 队列IO任务
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<bool> QueueIoAsync(Func<ValueTask> work, CancellationToken cancellationToken = default)
        {
            return _ioWriter.TryWrite(work) 
                ? new ValueTask<bool>(true)
                : QueueIoSlowAsync(work, cancellationToken);
        }
        
        /// <summary>
        /// 队列CPU任务
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<bool> QueueCpuAsync(Func<ValueTask> work, CancellationToken cancellationToken = default)
        {
            return _cpuWriter.TryWrite(work) 
                ? new ValueTask<bool>(true)
                : QueueCpuSlowAsync(work, cancellationToken);
        }
        
        // 慢路径 - 只在Channel满时才调用
        private async ValueTask<bool> QueueIoSlowAsync(Func<ValueTask> work, CancellationToken cancellationToken)
        {
            try
            {
                await _ioWriter.WriteAsync(work, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
        
        private async ValueTask<bool> QueueCpuSlowAsync(Func<ValueTask> work, CancellationToken cancellationToken)
        {
            try
            {
                await _cpuWriter.WriteAsync(work, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
        
        /// <summary>
        /// 同步版本 - 兼容旧接口
        /// </summary>
        public bool QueueIo(Action work) => QueueIoAsync(() => { work(); return ValueTask.CompletedTask; }).AsTask().Result;
        public bool QueueCpu(Action work) => QueueCpuAsync(() => { work(); return ValueTask.CompletedTask; }).AsTask().Result;
        
        /// <summary>
        /// 关闭
        /// </summary>
        public async Task CompleteAsync()
        {
            _ioWriter.Complete();
            _cpuWriter.Complete();
            await Task.WhenAll(_ioWorkers.Concat(_cpuWorkers));
        }
        
        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAll()
        {
            _cancellationTokenSource.Cancel();
            _ioWriter.Complete();
            _cpuWriter.Complete();
        }
        
        public void Dispose()
        {
            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                CancelAll();
            _cancellationTokenSource?.Dispose();
        }
    }
    
    /// <summary>
    /// 无锁异步手动重置事件
    /// </summary>
    public sealed class LockFreeAsyncManualResetEvent : IDisposable
    {
        // 状态编码：0=未设置，1=已设置，-1=已释放
        private volatile int _state;
        private volatile TaskCompletionSource<bool>? _tcs;
        
        // 对象池 - 减少TaskCompletionSource分配
        private static readonly ObjectPool<TaskCompletionSource<bool>> TcsPool = 
            new(() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        
        public LockFreeAsyncManualResetEvent(bool initialState = false)
        {
            _state = initialState ? 1 : 0;
            if (!initialState)
                _tcs = TcsPool.Get();
        }
        
        /// <summary>
        /// 事件是否已设置 - 无锁检查
        /// </summary>
        public bool IsSet
        {
            get
            {
                var state = _state;
                return Volatile.Read(ref state) == 1;
            }
        }
        
        /// <summary>
        /// 异步等待
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ValueTask WaitAsync(CancellationToken cancellationToken = default)
        {
            var state = _state;
            if (state == 1) return ValueTask.CompletedTask; // 快速路径
            if (state == -1) return ValueTask.FromException(new ObjectDisposedException(nameof(LockFreeAsyncManualResetEvent)));
            
            var currentTcs = _tcs;
            if (currentTcs == null) return ValueTask.CompletedTask;
            
            return cancellationToken.CanBeCanceled 
                ? new ValueTask(WaitWithCancellationAsync(currentTcs.Task, cancellationToken))
                : new ValueTask(currentTcs.Task);
        }
        
        /// <summary>
        /// 带取消的等待 - 优化分配
        /// </summary>
        private static async Task WaitWithCancellationAsync(Task waitTask, CancellationToken cancellationToken)
        {
            if (waitTask.IsCompleted) return;
            
            var cancellationTask = Task.Delay(-1, cancellationToken);
            var completedTask = await Task.WhenAny(waitTask, cancellationTask);
            
            if (completedTask == cancellationTask)
                cancellationToken.ThrowIfCancellationRequested();
        }
        
        /// <summary>
        /// 设置事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Set()
        {
            var oldState = Interlocked.Exchange(ref _state, 1);
            if (oldState == 1 || oldState == -1) return; // 已设置或已释放
            
            var currentTcs = Interlocked.Exchange(ref _tcs, null);
            if (currentTcs != null)
            {
                // 在后台线程完成，避免阻塞调用者
                ThreadPool.QueueUserWorkItem(static state =>
                {
                    var tcs = (TaskCompletionSource<bool>)state!;
                    tcs.TrySetResult(true);
                    TcsPool.Return(tcs);
                }, currentTcs);
            }
        }
        
        /// <summary>
        /// 重置事件
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Reset()
        {
            var oldState = _state;
            if (oldState == 0 || oldState == -1) return; // 已重置或已释放
            
            var newTcs = TcsPool.Get();
            var oldTcs = Interlocked.Exchange(ref _tcs, newTcs);
            
            if (Interlocked.CompareExchange(ref _state, 0, 1) == 1)
            {
                // 成功重置
                if (oldTcs != null)
                {
                    ThreadPool.QueueUserWorkItem(static state =>
                    {
                        var tcs = (TaskCompletionSource<bool>)state!;
                        tcs.TrySetCanceled();
                        TcsPool.Return(tcs);
                    }, oldTcs);
                }
            }
            else
            {
                // 重置失败，返回TCS到池
                Interlocked.Exchange(ref _tcs, oldTcs);
                TcsPool.Return(newTcs);
            }
        }
        
        public void Dispose()
        {
            var oldState = Interlocked.Exchange(ref _state, -1);
            if (oldState == -1) return; // 已释放
            
            var currentTcs = Interlocked.Exchange(ref _tcs, null);
            if (currentTcs != null)
            {
                ThreadPool.QueueUserWorkItem(static state =>
                {
                    var tcs = (TaskCompletionSource<bool>)state!;
                    tcs.TrySetException(new ObjectDisposedException(nameof(LockFreeAsyncManualResetEvent)));
                    TcsPool.Return(tcs);
                }, currentTcs);
            }
        }
    }
    
    /// <summary>
    /// 对象池
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private sealed class Node
        {
            public Node? Next;
            public T Item = default!; // 必须在使用前设置
        }
        
        private Node? _head;
        private readonly Func<T> _factory;
        private readonly Action<T>? _reset;
        private volatile int _count;
        private readonly int _maxSize;
        
        public int Count => _count;
        
        public ObjectPool(Func<T> factory, Action<T>? reset = null, int maxSize = 100)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _maxSize = maxSize;
        }
        
        /// <summary>
        /// 获取对象 - 无锁操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public T Get()
        {
            var head = Volatile.Read(ref _head);
            if (head != null && Interlocked.CompareExchange(ref _head, head.Next, head) == head)
            {
                Interlocked.Decrement(ref _count);
                return head.Item;
            }
            
            return _factory();
        }
        
        /// <summary>
        /// 返回对象 - 无锁操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Return(T item)
        {
            if (item == null || _count >= _maxSize) return;
            
            _reset?.Invoke(item);
            
            var newNode = new Node { Item = item };
            do
            {
                newNode.Next = Volatile.Read(ref _head);
            } while (Interlocked.CompareExchange(ref _head, newNode, newNode.Next) != newNode.Next);
            
            Interlocked.Increment(ref _count);
        }
        
        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            Interlocked.Exchange(ref _head, null);
            Interlocked.Exchange(ref _count, 0);
        }
    }
    
    /// <summary>
    /// 高性能异步信号量 - 支持零分配路径
    /// </summary>
    public sealed class AsyncSemaphore : IDisposable
    {
        private volatile int _currentCount;
        private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
        private volatile bool _disposed;
        
        public int CurrentCount => _currentCount;
        
        public AsyncSemaphore(int initialCount, int maxCount = int.MaxValue)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException(nameof(initialCount));
            _currentCount = Math.Min(initialCount, maxCount);
        }
        
        /// <summary>
        /// 异步等待信号量 - 零分配快速路径
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public ValueTask WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return ValueTask.FromException(new ObjectDisposedException(nameof(AsyncSemaphore)));
            
            // 尝试快速获取
            var count = _currentCount;
            if (count > 0 && Interlocked.CompareExchange(ref _currentCount, count - 1, count) == count)
                return ValueTask.CompletedTask;
            
            // 慢路径
            return WaitSlowAsync(cancellationToken);
        }
        
        private async ValueTask WaitSlowAsync(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(tcs);
            
            // 再次检查是否有可用许可
            if (TryAcquire() && _waiters.TryDequeue(out var dequeuedTcs) && dequeuedTcs == tcs)
            {
                tcs.SetResult(true);
                return;
            }
            
            if (cancellationToken.CanBeCanceled)
            {
                using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
                await tcs.Task;
            }
            else
            {
                await tcs.Task;
            }
        }
        
        /// <summary>
        /// 释放信号量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Release(int releaseCount = 1)
        {
            if (releaseCount <= 0) throw new ArgumentOutOfRangeException(nameof(releaseCount));
            if (_disposed) return;
            
            for (int i = 0; i < releaseCount; i++)
            {
                if (_waiters.TryDequeue(out var tcs))
                {
                    ThreadPool.QueueUserWorkItem(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), tcs);
                }
                else
                {
                    Interlocked.Increment(ref _currentCount);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquire()
        {
            var count = _currentCount;
            return count > 0 && Interlocked.CompareExchange(ref _currentCount, count - 1, count) == count;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            // 取消所有等待者
            while (_waiters.TryDequeue(out var tcs))
                ThreadPool.QueueUserWorkItem(static state => ((TaskCompletionSource<bool>)state!).TrySetCanceled(), tcs);
        }
    }
}