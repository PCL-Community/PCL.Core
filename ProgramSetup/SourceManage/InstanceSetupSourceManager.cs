using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.ProgramSetup.SourceManage;

public sealed class InstanceSetupSourceManager : ISetupSourceManager, IDisposable
{
    #region ISetupSourceManager

    public string? Get(string key, string? gamePath)
    {
        if (gamePath is null)
            throw new ArgumentException("获取游戏实例配置时未提供游戏路径", nameof(gamePath));
        string? result = default;
        _UseCache(_GetFilePath(gamePath), cache =>
        {
            result = cache.Content.TryGetValue(key, out var value) ? value : null;
        });
        return result;
    }

    public string? Set(string key, string value, string? gamePath)
    {
        if (gamePath is null)
            throw new ArgumentException("获取游戏实例配置时未提供游戏路径", nameof(gamePath));
        string? result = default;
        _UseCache(_GetFilePath(gamePath), cache =>
        {
            result = cache.Content.UpdateAndGetPrevious(key, value);
            _waitingToSave.Add(cache);
        });
        return result;
    }

    public string? Remove(string key, string? gamePath)
    {
        if (gamePath is null)
            throw new ArgumentException("获取游戏实例配置时未提供游戏路径", nameof(gamePath));
        string? result = default;
        _UseCache(_GetFilePath(gamePath), cache =>
        {
            result = cache.Content.TryRemove(key, out var value) ? value : null;
            _waitingToSave.Add(cache);
        });
        return result;
    }

    #endregion

    private readonly IFileSerializer<ConcurrentDictionary<string, string>> _serializer;
    private readonly Dictionary<string, CacheEntry> _fileCache = new();
    private readonly Thread _saveJobThread;
    private readonly ProducerConsumerSet<CacheEntry> _waitingToSaveUnderlying = new();
    private readonly BlockingCollection<CacheEntry> _waitingToSave;
    private volatile int _disposed = 0;

    public InstanceSetupSourceManager(IFileSerializer<ConcurrentDictionary<string, string>> serializer)
    {
        _serializer = serializer;
        _waitingToSave = new BlockingCollection<CacheEntry>(_waitingToSaveUnderlying);
        _saveJobThread = new Thread(_SaveJob);
        _saveJobThread.Start();
    }

    private static string _GetFilePath(string gamePath) => Path.Combine(gamePath, "PCL", "setup.ini");

    private void _UseCache(string filePath, Action<CacheEntry> action)
    {
        lock (_fileCache)
        {
            // 尝试获取已有的缓存
            if (!_fileCache.TryGetValue(filePath, out var cache))
            {
                // 新加载文件内容
                ConcurrentDictionary<string, string> content;
                try
                {
                    var dir = Path.GetDirectoryName(filePath);
                    if (dir is not null && dir.Length > 0)
                        Directory.CreateDirectory(dir);
                    using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
                    content = _serializer.Deserialize(fs);
                }
                catch (Exception ex)
                {
                    throw new IOException("加载配置文件内容失败：" + filePath, ex);
                }
                // 保存缓存
                cache = _fileCache[filePath] = new CacheEntry(filePath, content);
                LogWrapper.Debug("Setup", "从硬盘加载游戏实例配置文件：" + cache.FilePath);
            }
            action.Invoke(cache);
        }
    }

    private void _SaveJob()
    {
        while (true)
        {
            CacheEntry cache;
            try { cache = _waitingToSave.Take(); }
            catch (InvalidOperationException) { break; /* 完事，收工 */ }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Setup", "处理实例保存队列时的意外错误");
                break; // 瘫了
            }
            try
            {
                // 检查文件是否已被修改
                if (!cache.VerifyLastWriteTime())
                {
                    LogWrapper.Error("Setup", "配置模型已损坏：尝试保存一个游戏实例文件夹内的配置文件，但它已被外部修改");
                    lock (_fileCache)
                        _fileCache.Remove(cache.FilePath);
                    continue;
                }
                // 写入临时文件
                var dir = Path.GetDirectoryName(cache.FilePath);
                if (dir is not null && dir.Length > 0)
                    Directory.CreateDirectory(dir);
                var tmpPath = cache.FilePath + ".tmp";
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
                    _serializer.Serialize(cache.Content, fs);
                // 替换文件
                File.Replace(tmpPath, cache.FilePath, null);
                // 如果没要求保存这个缓存就先删掉缓存，下次使用时再加载
                lock (_fileCache)
                {
                    if (!_waitingToSaveUnderlying.Contains(cache))
                        _fileCache.Remove(cache.FilePath);
                }
                LogWrapper.Debug("Setup", "向硬盘同步游戏实例配置文件：" + cache.FilePath);
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Setup", "向硬盘同步配置文件失败：" + cache.FilePath);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _waitingToSave.CompleteAdding();
        _saveJobThread.Join();
        _waitingToSave.Dispose();
        _fileCache.Clear();
    }

    private sealed class CacheEntry(string filePath, ConcurrentDictionary<string, string> content)
    {
        public readonly string FilePath = filePath;
        public readonly ConcurrentDictionary<string, string> Content = content;
        private volatile object _lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        public bool VerifyLastWriteTime() => File.GetLastWriteTimeUtc(FilePath) == (DateTime) _lastWriteTime;
        public void UpdateLastWriteTime() => _lastWriteTime = File.GetLastWriteTimeUtc(FilePath);
    }

    /// <summary>
    /// 一个用于应付 BlockingCollection 的会自动去重的并发集合
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    private sealed class ProducerConsumerSet<T> : IProducerConsumerCollection<T>
    {
        private readonly ConcurrentDictionary<T, object?> _dictionary = new();

        public bool Contains(T item) => _dictionary.ContainsKey(item);

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            _dictionary.TryAdd(item, null);
            return true;
        }

        bool IProducerConsumerCollection<T>.TryTake([UnscopedRef] out T item)
        {
            while (true)
            {
                var pair = _dictionary.FirstOrDefault();
                if (pair.Key is null)
                {
                    item = default!;
                    return false;
                }
                if (_dictionary.TryRemove(pair.Key, out _))
                {
                    item = pair.Key;
                    return true;
                }
            }
        }

        // ReSharper disable once NotDisposedResourceIsReturned
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _dictionary.Keys.GetEnumerator();

        // ReSharper disable once NotDisposedResourceIsReturned
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.Keys.GetEnumerator();
        int ICollection.Count => _dictionary.Count;
        object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;
        bool ICollection.IsSynchronized => ((ICollection)_dictionary).IsSynchronized;
        void ICollection.CopyTo(Array array, int index) => _dictionary.Keys.CopyTo((T[])array, index);
        void IProducerConsumerCollection<T>.CopyTo(T[] array, int index) => _dictionary.Keys.CopyTo(array, index);
        T[] IProducerConsumerCollection<T>.ToArray() => _dictionary.Keys.ToArray();
    }
}

