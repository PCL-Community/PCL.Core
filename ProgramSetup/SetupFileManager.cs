using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.ProgramSetup;

public sealed class SetupFileManager : ISetupFileManager, IDisposable
{
    private readonly string _filePath;
    private readonly ISetupFileSerializer _serializer;
    private readonly ReaderWriterLockSlim _rwLock = new();
    private readonly ManualResetEventSlim _saveEvent = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _saveTask;
    private ConcurrentDictionary<string, string> _content = new();
    private bool _disposed = false;

    public SetupFileManager(string filePath, ISetupFileSerializer serializer)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _saveTask = new Task(() => Save(_cts.Token), TaskCreationOptions.LongRunning);
        _saveTask.Start();
        Load();
    }

    string? ISetupFileManager.this[string key, string? mcPath]
    {
        get => this[key];
        set => this[key] = value;
    }

    public string? this[string key]
    {
        get
        {
            _content.TryGetValue(key, out string? value);
            return value;
        }
        set
        {
            if (value is not null)
            {
                // set
                _rwLock.EnterReadLock();
                _content.AddOrUpdate(key, value, (_, _) => value);
                _saveEvent.Set();
                _rwLock.EnterReadLock();
            }
            else
            {
                // delete
                _rwLock.EnterReadLock();
                _content.TryRemove(key, out _);
                _saveEvent.Set();
                _rwLock.EnterReadLock();
            }
        }
    }

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
            var jResult = _serializer.Deserialize(fs);
            _content = jResult ?? throw new NullReferenceException(nameof(jResult) + " is null");
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