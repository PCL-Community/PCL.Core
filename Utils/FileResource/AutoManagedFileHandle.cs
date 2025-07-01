using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Helper;

namespace PCL.Core.Utils.FileResource;

public sealed class AutoManagedFileHandle : IDisposable
{
    private readonly string _filePath;
    private readonly ManualResetEventSlim _autoWriteEvent = new();
    private readonly CancellationTokenSource _autoWriteCts = new();
    private readonly Task _autoWriteTask;
    private readonly ReaderWriterLockSlim _rwLock = new();
    private Action<string> _onDispose;
    private bool _disposed = false;

    public AutoManagedFileHandle(string filePath, Action<string> onDispose)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
        EnsureDirectoryCreated(_filePath);
        _autoWriteTask = new Task(AutoWriteJob, TaskCreationOptions.LongRunning);
        _autoWriteTask.Start();
    }

    public event Action<Stream>? WriteToFile;

    public void DoReadOperation(Action<string> readCallback)
    {
        _rwLock.EnterUpgradeableReadLock();
        try
        {
            EnsureDirectoryCreated(_filePath);
            readCallback.Invoke(_filePath);
        }
        finally
        {
            _rwLock.ExitUpgradeableReadLock();
        }
    }

    public void NotifyAutoWrite()
    {
        _rwLock.EnterReadLock();
        try
        {
            _autoWriteEvent.Set();
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    private void AutoWriteJob()
    {
        while (true)
        {
            try
            {
                _autoWriteEvent.Wait(_autoWriteCts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!_autoWriteEvent.IsSet)
                    break;
            }
            _rwLock.EnterWriteLock();
            try
            {
                _autoWriteEvent.Reset();
                EnsureDirectoryCreated(_filePath);
                using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                WriteToFile?.Invoke(fs);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
    }

    private static void EnsureDirectoryCreated(string filePath)
    {
        try
        {
            if (Path.GetDirectoryName(filePath) is { Length: > 0 } dir)
                Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "FileManage", "创建文件夹失败：" + filePath);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _autoWriteCts.Cancel();
        _autoWriteTask.Wait();
        _autoWriteCts.Dispose();
        _autoWriteTask.Dispose();
        _autoWriteEvent.Dispose();
        _rwLock.Dispose();
        _onDispose.Invoke(_filePath);
        _onDispose = null!;
    }
}