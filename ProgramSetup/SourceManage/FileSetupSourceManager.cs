using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.ProgramSetup.SourceManage;

public sealed class FileSetupSourceManager : ISetupSourceManager, IDisposable
{
    #region ISetupSourceManager

    public string? Get(string key, string? gamePath = null)
    {
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        return _content.TryGetValue(key, out var value) ? value : null;
    }

    public string? Set(string key, string value, string? gamePath = null)
    {
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        var result = _content.UpdateAndGetPrevious(key, value);
        _saveEvent.Set();
        return result;
    }

    public string? Remove(string key, string? gamePath = null)
    {
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        var result = _content.TryRemove(key, out var value) ? value : null;
        _saveEvent.Set();
        return result;
    }

    #endregion

    private readonly ConcurrentDictionary<string, string> _content;
    private readonly IFileSerializer<ConcurrentDictionary<string, string>> _serializer;
    private readonly FileItem _baseFile;
    private readonly Thread _saveJobThread;
    private readonly CancellationTokenSource _saveJobCts = new();
    private readonly ManualResetEventSlim _saveEvent = new();
    private volatile int _disposed = 0;

    public FileSetupSourceManager(FileItem baseFile, IFileSerializer<ConcurrentDictionary<string, string>> serializer,
        bool contentPreloaded)
    {
        _serializer = serializer;
        _baseFile = baseFile;
        if (contentPreloaded)
        {
            // 获取预加载的文件内容
            var fileResult = FileService.WaitForResult(baseFile, TimeSpan.FromSeconds(10));
            if (fileResult is null)
                throw new InvalidOperationException("获取预加载内容失败：" + _baseFile);
            _content = fileResult.Value<ConcurrentDictionary<string, string>>();
        }
        else
        {
            // 从硬盘加载文件内容
            try
            {
                _baseFile.CreateDirectory();
                using var fs = new FileStream(_baseFile.TargetPath, FileMode.OpenOrCreate, FileAccess.Read);
                _content = _serializer.Deserialize(fs);
            }
            catch (Exception ex)
            {
                throw new IOException("加载配置文件内容失败：" + _baseFile.TargetPath, ex);
            }
        }
        // 启动保存工作线程
        _saveJobThread = new Thread(_SaveJob) { Priority = ThreadPriority.BelowNormal };
        _saveJobThread.Start();
    }

    private void _SaveJob()
    {
        while (true)
        {
            try
            {
                // 等待要求保存内容……
                _saveEvent.Wait(_saveJobCts.Token);
            }
            catch (OperationCanceledException) when (_saveEvent.IsSet) { /* 已被 dispose，但需要保存，再保存一次 */ }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Setup", "等待保存事件时的意外错误");
                break; // 瘫了
            }
            try
            {
                // 收到保存请求！
                try { Task.Delay(2000, _saveJobCts.Token).Wait(); } // 磨洋工
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException) {  }
                _saveEvent.Reset();
                // 写入临时文件
                var targetPath = _baseFile.TargetPath;
                var tmpPath = targetPath + ".tmp";
                _baseFile.CreateDirectory();
                using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    _serializer.Serialize(_content, fs);
                // 替换文件
                File.Replace(tmpPath, targetPath, null);
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Setup", "向硬盘同步配置文件失败：" + _baseFile.TargetPath);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _saveJobCts.Cancel();
        _saveJobThread.Join(); // 等待保存工作线程结束
        _saveJobCts.Dispose();
        _saveEvent.Dispose();
    }
}