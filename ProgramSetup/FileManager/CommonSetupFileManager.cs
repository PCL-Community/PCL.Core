using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.ProgramSetup.FileManager;

/// <summary>
/// 用于托管某个特定的设置文件的类，会异步地写入文件
/// </summary>
public sealed class CommonSetupFileManager : ISetupFileManager, IDisposable
{
    private readonly string _filePath;
    private readonly ISetupFileSerializer _serializer;
    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly ManualResetEventSlim _saveEvent = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _saveTask;
    private ConcurrentDictionary<string, string> _content = new();
    private bool _disposed = false;

    public CommonSetupFileManager(string filePath, ISetupFileSerializer serializer)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _saveTask = new Task(() => Save(_cts.Token), TaskCreationOptions.LongRunning);
        _saveTask.Start();
        Load();
    }

    public string? Get(string key, string? mcPath)
    {
        _content.TryGetValue(key, out string? value);
        return value;
    }

    public string? Set(string key, string value, string? mcPath)
    {
        string? result = null;
        _rwLock.EnterReadLock();
        _content.AddOrUpdate(key, _ =>
        {
            result = null;
            return value;
        }, (_, existingValue) =>
        {
            result = existingValue;
            return value;
        });
        _saveEvent.Set();
        _rwLock.ExitReadLock();
        return result;
    }

    public string? Remove(string key, string? mcPath)
    {
        _rwLock.EnterReadLock();
        _content.TryRemove(key, out string? value);
        _saveEvent.Set();
        _rwLock.ExitReadLock();
        return value;
    }

    /// <summary>
    /// 开始多重操作，在操作中暂缓对文件的写入，待操作结束后再写入文件，用于在更新多个配置项时的性能优化。<br/>
    /// note：不支持跨线程
    /// </summary>
    public IDisposable BeginMultipleOperation()
    {
        var threadId = Environment.CurrentManagedThreadId;
        _rwLock.EnterReadLock();
        _saveEvent.Set();
        return new CallbackDisposable(() =>
        {
            if (Environment.CurrentManagedThreadId != threadId)
                throw new InvalidOperationException("必须在同一线程进入与退出多重操作");
            _rwLock.ExitReadLock();
        });
    }

    private void Load()
    {
        _rwLock.EnterWriteLock();
        try
        {
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            var deserialized = _serializer.Deserialize(fs);
            _content = deserialized ?? new ConcurrentDictionary<string, string>();
        }
        catch (Exception ex)
        {
            throw new Exception($"将文件解析为配置文件失败：{_filePath}", ex);
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    private void Save(CancellationToken token)
    {
        while (true)
        {
            try
            {
                _saveEvent.Wait(token);
            }
            catch (OperationCanceledException)
            {
                if (!_saveEvent.IsSet)
                    break;
            }
            _rwLock.EnterWriteLock();
            _saveEvent.Reset();
            string jResult = _serializer.Serialize(_content);
            _rwLock.ExitWriteLock();
            try
            {
                var tmpFile = _filePath + ".tmp";
                var bakFile = _filePath + ".bak";
                File.WriteAllText(tmpFile, jResult);
                File.Replace(tmpFile, _filePath, bakFile);
            }
            catch (Exception ex)
            {
                _saveEvent.Dispose();
                throw new Exception($"向硬盘同步文件失败：{_filePath}", ex);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _cts.Cancel();
        _saveTask.Wait();
        _rwLock.Dispose();
        _cts.Dispose();
        _saveEvent.Dispose();
        _saveTask.Dispose();
    }
}