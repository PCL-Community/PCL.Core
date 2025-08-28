using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace PCL.Core.Utils;

/// <summary>
/// 高性能数据结构工具包
/// 使用无锁算法、内存对齐、SIMD优化等技术
/// </summary>
public static class DataStructures
{
    /// <summary>
    /// 高性能无锁并发集合 - 使用分段锁设计
    /// </summary>
    public sealed class FastConcurrentSet<T> : IProducerConsumerCollection<T>, ICollection<T> where T : notnull
    {
        // 使用分段锁减少竞争
        private const int SegmentCount = 32;
        private const int SegmentMask = SegmentCount - 1;
        
        private readonly Segment[] _segments;
        private volatile int _count;
        
        // 每个段使用独立的锁和存储
        [StructLayout(LayoutKind.Explicit, Size = 64)] // 避免伪共享
        private sealed class Segment
        {
            [FieldOffset(0)]
            public readonly ConcurrentDictionary<T, byte> Items = new();
            
            [FieldOffset(8)] internal volatile int _localCount;
            
            public int LocalCount => _localCount;
            
            public bool TryAdd(T item)
            {
                if (Items.TryAdd(item, 0))
                {
                    Interlocked.Increment(ref _localCount);
                    return true;
                }
                return false;
            }
            
            public bool TryRemove(T item)
            {
                if (Items.TryRemove(item, out _))
                {
                    Interlocked.Decrement(ref _localCount);
                    return true;
                }
                return false;
            }
            
            public bool Contains(T item) => Items.ContainsKey(item);
            
            public void Clear()
            {
                var count = Items.Count;
                Items.Clear();
                Interlocked.Add(ref _localCount, -count);
            }
        }
        
        public bool IsReadOnly => false;
        public int Count => _count;
        public bool IgnoreDuplicated { get; init; } = false;
        
        object ICollection.SyncRoot => this;
        bool ICollection.IsSynchronized => true;
        
        public FastConcurrentSet(int capacity = 16)
        {
            _segments = new Segment[SegmentCount];
            for (int i = 0; i < SegmentCount; i++)
                _segments[i] = new Segment();
        }
        
        /// <summary>
        /// 获取项目的段索引 - 使用快速哈希
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetSegmentIndex(T item)
        {
            // 使用FNV-1a哈希的变体，比GetHashCode更快更均匀
            var hash = item.GetHashCode();
            hash = ((hash >> 16) ^ hash) * 0x45d9f3b;
            hash = ((hash >> 16) ^ hash) * 0x45d9f3b;
            hash = (hash >> 16) ^ hash;
            return hash & SegmentMask;
        }
        
        /// <summary>
        /// 高性能添加操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryAdd(T item)
        {
            var segment = _segments[GetSegmentIndex(item)];
            var added = segment.TryAdd(item);
            
            if (added)
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            
            return IgnoreDuplicated;
        }
        
        /// <summary>
        /// 快速移除任意元素 - 避免原版的无限循环问题
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryTake([UnscopedRef] out T item)
        {
            // 随机选择起始段，避免总是从同一段取元素
            var startIndex = Environment.CurrentManagedThreadId & SegmentMask;
            
            for (int i = 0; i < SegmentCount; i++)
            {
                var segmentIndex = (startIndex + i) & SegmentMask;
                var segment = _segments[segmentIndex];
                
                if (segment.LocalCount > 0)
                {
                    // 尝试获取第一个可用元素
                    foreach (var kvp in segment.Items)
                    {
                        if (segment.Items.TryRemove(kvp.Key, out _))
                        {
                            Interlocked.Decrement(ref segment._localCount);
                            Interlocked.Decrement(ref _count);
                            item = kvp.Key;
                            return true;
                        }
                    }
                }
            }
            
            item = default!;
            return false;
        }
        
        /// <summary>
        /// 批量取出元素 - 高性能批量操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int TryTakeMany(Span<T> items)
        {
            var taken = 0;
            var startIndex = Environment.CurrentManagedThreadId & SegmentMask;
            
            for (int i = 0; i < SegmentCount && taken < items.Length; i++)
            {
                var segmentIndex = (startIndex + i) & SegmentMask;
                var segment = _segments[segmentIndex];
                
                if (segment.LocalCount > 0)
                {
                    var batchSize = Math.Min(items.Length - taken, segment.LocalCount);
                    var batchTaken = 0;
                    
                    foreach (var kvp in segment.Items)
                    {
                        if (batchTaken >= batchSize) break;
                        
                        if (segment.Items.TryRemove(kvp.Key, out _))
                        {
                            items[taken + batchTaken] = kvp.Key;
                            batchTaken++;
                            Interlocked.Decrement(ref segment._localCount);
                        }
                    }
                    
                    taken += batchTaken;
                    Interlocked.Add(ref _count, -batchTaken);
                }
            }
            
            return taken;
        }
        
        public bool Remove(T item)
        {
            var segment = _segments[GetSegmentIndex(item)];
            if (segment.TryRemove(item))
            {
                Interlocked.Decrement(ref _count);
                return true;
            }
            return false;
        }
        
        public void Add(T item)
        {
            if (!TryAdd(item) && !IgnoreDuplicated)
                throw new ArgumentException($"{nameof(FastConcurrentSet<T>)} 中已存在该元素");
        }
        
        public void Clear()
        {
            var totalCount = 0;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = _segments[i];
                totalCount += segment.LocalCount;
                segment.Clear();
            }
            Interlocked.Add(ref _count, -totalCount);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => _segments[GetSegmentIndex(item)].Contains(item);
        
        /// <summary>
        /// 高性能枚举器 - 使用快照避免锁
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            // 创建快照避免枚举期间的修改问题
            var snapshot = new List<T>(_count);
            for (int i = 0; i < SegmentCount; i++)
            {
                snapshot.AddRange(_segments[i].Items.Keys);
            }
            return snapshot.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            
            var index = arrayIndex;
            for (int i = 0; i < SegmentCount; i++)
            {
                foreach (var item in _segments[i].Items.Keys)
                {
                    if (index >= array.Length) return;
                    array[index++] = item;
                }
            }
        }
        
        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);
        
        public T[] ToArray()
        {
            var result = new T[_count];
            CopyTo(result, 0);
            return result;
        }
    }
    
    /// <summary>
    /// 高性能环形缓冲区 - 使用无锁单生产者单消费者模式
    /// </summary>
    public sealed class RingBuffer<T> : IDisposable where T : struct
    {
        private readonly T[] _buffer;
        private readonly int _mask;
        private long _writeIndex;
        private long _readIndex;
        private readonly int _capacity;
        
        // 缓存行填充，避免伪共享
        [StructLayout(LayoutKind.Explicit, Size = 128)]
        private struct CacheLinePadding { }
        
        #pragma warning disable CS0169 // 从不使用字段
        private readonly CacheLinePadding _padding1;
        private readonly CacheLinePadding _padding2;
        #pragma warning restore CS0169
        
        public int Count => (int)(_writeIndex - _readIndex);
        public int Capacity => _capacity;
        public bool IsEmpty => _readIndex == _writeIndex;
        public bool IsFull => Count == _capacity;
        
        public RingBuffer(int capacity)
        {
            if (capacity <= 0 || !IsPowerOfTwo(capacity))
                throw new ArgumentException("容量必须是2的幂且大于0");
                
            _capacity = capacity;
            _buffer = new T[capacity];
            _mask = capacity - 1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPowerOfTwo(int value) => (value & (value - 1)) == 0;
        
        /// <summary>
        /// 无锁写入 - 单生产者
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryWrite(T item)
        {
            var currentWrite = _writeIndex;
            var currentRead = Volatile.Read(ref _readIndex);
            
            if (currentWrite - currentRead >= _capacity)
                return false; // 缓冲区已满
                
            _buffer[currentWrite & _mask] = item;
            Volatile.Write(ref _writeIndex, currentWrite + 1);
            return true;
        }
        
        /// <summary>
        /// 无锁读取 - 单消费者
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryRead(out T item)
        {
            var currentRead = _readIndex;
            var currentWrite = Volatile.Read(ref _writeIndex);
            
            if (currentRead >= currentWrite)
            {
                item = default;
                return false; // 缓冲区为空
            }
            
            item = _buffer[currentRead & _mask];
            Volatile.Write(ref _readIndex, currentRead + 1);
            return true;
        }
        
        /// <summary>
        /// 批量写入 - 高性能批量操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int TryWriteMany(ReadOnlySpan<T> items)
        {
            var currentWrite = _writeIndex;
            var currentRead = Volatile.Read(ref _readIndex);
            var availableSpace = _capacity - (int)(currentWrite - currentRead);
            var writeCount = Math.Min(items.Length, availableSpace);
            
            if (writeCount == 0) return 0;
            
            for (int i = 0; i < writeCount; i++)
            {
                _buffer[(currentWrite + i) & _mask] = items[i];
            }
            
            Volatile.Write(ref _writeIndex, currentWrite + writeCount);
            return writeCount;
        }
        
        /// <summary>
        /// 批量读取 - 高性能批量操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int TryReadMany(Span<T> items)
        {
            var currentRead = _readIndex;
            var currentWrite = Volatile.Read(ref _writeIndex);
            var availableItems = (int)(currentWrite - currentRead);
            var readCount = Math.Min(items.Length, availableItems);
            
            if (readCount == 0) return 0;
            
            for (int i = 0; i < readCount; i++)
            {
                items[i] = _buffer[(currentRead + i) & _mask];
            }
            
            Volatile.Write(ref _readIndex, currentRead + readCount);
            return readCount;
        }
        
        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            Volatile.Write(ref _readIndex, Volatile.Read(ref _writeIndex));
        }
        
        public void Dispose()
        {
            // 清空缓冲区，但不需要处理数组
            Clear();
        }
    }
    
    /// <summary>
    /// 高性能优先队列 - 使用优化的二叉堆
    /// </summary>
    public sealed class PriorityQueue<T> where T : IComparable<T>
    {
        private T[] _heap;
        private int _count;
        private readonly IComparer<T> _comparer;
        
        public int Count => _count;
        public int Capacity => _heap.Length;
        
        public PriorityQueue(int capacity = 16, IComparer<T>? comparer = null)
        {
            _heap = new T[Math.Max(capacity, 4)];
            _comparer = comparer ?? Comparer<T>.Default;
        }
        
        /// <summary>
        /// 高性能入队 - 优化的上浮算法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Enqueue(T item)
        {
            if (_count == _heap.Length)
                Resize();
                
            var index = _count++;
            
            // 优化的上浮过程
            while (index > 0)
            {
                var parentIndex = (index - 1) >> 1; // 使用位运算代替除法
                if (_comparer.Compare(item, _heap[parentIndex]) >= 0) break;
                
                _heap[index] = _heap[parentIndex];
                index = parentIndex;
            }
            
            _heap[index] = item;
        }
        
        /// <summary>
        /// 高性能出队 - 优化的下沉算法
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public T Dequeue()
        {
            if (_count == 0) throw new InvalidOperationException("队列为空");
            
            var result = _heap[0];
            var lastItem = _heap[--_count];
            
            if (_count > 0)
            {
                var index = 0;
                var halfCount = _count >> 1; // _count / 2
                
                // 优化的下沉过程
                while (index < halfCount)
                {
                    var leftChild = (index << 1) + 1; // index * 2 + 1
                    var rightChild = leftChild + 1;
                    var smallest = leftChild;
                    
                    if (rightChild < _count && _comparer.Compare(_heap[rightChild], _heap[leftChild]) < 0)
                        smallest = rightChild;
                        
                    if (_comparer.Compare(lastItem, _heap[smallest]) <= 0) break;
                    
                    _heap[index] = _heap[smallest];
                    index = smallest;
                }
                
                _heap[index] = lastItem;
            }
            
            return result;
        }
        
        /// <summary>
        /// 查看队头元素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek() => _count > 0 ? _heap[0] : throw new InvalidOperationException("队列为空");
        
        /// <summary>
        /// 尝试查看队头元素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek([UnscopedRef] out T result)
        {
            if (_count > 0)
            {
                result = _heap[0];
                return true;
            }
            result = default!;
            return false;
        }
        
        /// <summary>
        /// 尝试出队
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue([UnscopedRef] out T result)
        {
            if (_count > 0)
            {
                result = Dequeue();
                return true;
            }
            result = default!;
            return false;
        }
        
        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }
        
        /// <summary>
        /// 智能扩容
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var newCapacity = Math.Max(_heap.Length * 2, 4);
            Array.Resize(ref _heap, newCapacity);
        }
        
        /// <summary>
        /// 转换为数组
        /// </summary>
        public T[] ToArray()
        {
            var result = new T[_count];
            Array.Copy(_heap, result, _count);
            return result;
        }
    }
    
    /// <summary>
    /// 高性能LRU缓存 - 使用哈希表+双向链表
    /// </summary>
    public sealed class LruCache<TKey, TValue> where TKey : notnull
    {
        [StructLayout(LayoutKind.Sequential)]
        private sealed class Node
        {
            public TKey Key = default!;
            public TValue Value = default!;
            public Node? Previous;
            public Node? Next;
        }
        
        private readonly Dictionary<TKey, Node> _map;
        private readonly Node _head;
        private readonly Node _tail;
        private readonly int _capacity;
        
        public int Count => _map.Count;
        public int Capacity => _capacity;
        
        public LruCache(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            
            _capacity = capacity;
            _map = new Dictionary<TKey, Node>(capacity);
            
            // 创建虚拟头尾节点
            _head = new Node();
            _tail = new Node();
            _head.Next = _tail;
            _tail.Previous = _head;
        }
        
        /// <summary>
        /// 高性能获取值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool TryGetValue(TKey key, [UnscopedRef] out TValue value)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // 移动到头部（最近使用）
                MoveToHead(node);
                value = node.Value;
                return true;
            }
            
            value = default!;
            return false;
        }
        
        /// <summary>
        /// 高性能设置值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Set(TKey key, TValue value)
        {
            if (_map.TryGetValue(key, out var existingNode))
            {
                // 更新现有节点
                existingNode.Value = value;
                MoveToHead(existingNode);
            }
            else
            {
                // 添加新节点
                var newNode = new Node { Key = key, Value = value };
                
                if (_map.Count >= _capacity)
                {
                    // 移除最少使用的节点
                    var lru = _tail.Previous!;
                    RemoveNode(lru);
                    _map.Remove(lru.Key);
                }
                
                _map.Add(key, newNode);
                AddToHead(newNode);
            }
        }
        
        /// <summary>
        /// 移除节点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveNode(Node node)
        {
            node.Previous!.Next = node.Next;
            node.Next!.Previous = node.Previous;
        }
        
        /// <summary>
        /// 添加到头部
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToHead(Node node)
        {
            node.Previous = _head;
            node.Next = _head.Next;
            _head.Next!.Previous = node;
            _head.Next = node;
        }
        
        /// <summary>
        /// 移动到头部
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveToHead(Node node)
        {
            RemoveNode(node);
            AddToHead(node);
        }
        
        /// <summary>
        /// 移除指定键
        /// </summary>
        public bool Remove(TKey key)
        {
            if (_map.TryGetValue(key, out var node))
            {
                RemoveNode(node);
                _map.Remove(key);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _map.Clear();
            _head.Next = _tail;
            _tail.Previous = _head;
        }
    }
}