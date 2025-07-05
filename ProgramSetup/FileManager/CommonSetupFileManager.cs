using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Helper;

namespace PCL.Core.ProgramSetup.FileManager;

/// <summary>
/// 用于托管某个特定的设置文件的类，会异步地写入文件
/// </summary>
public sealed class CommonSetupFileManager : ISetupFileManager
{
    private readonly string _filePath;
    private readonly ISetupFileSerializer _serializer;
    private readonly ManualResetEventSlim _saveEvent = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _saveTask;
    private readonly ConcurrentDictionary<string, string> _content;
    private bool _disposed = false;

    public CommonSetupFileManager(string filePath, ISetupFileSerializer serializer)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _saveTask = new Task(() => Save(_cts.Token), TaskCreationOptions.LongRunning);
        _saveTask.Start();
        _content = Load();
    }

    public string? Get(string key, string? mcPath)
    {
        _content.TryGetValue(key, out string? value);
        return value;
    }

    public string? Set(string key, string value, string? mcPath)
    {
        string? result = null;
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
        return result;
    }

    public string? Remove(string key, string? mcPath)
    {
        _content.TryRemove(key, out string? value);
        _saveEvent.Set();
        return value;
    }

    private ConcurrentDictionary<string, string> Load()
    {
        try
        {
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            using var fs = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            return _serializer.Deserialize(fs) ?? new ConcurrentDictionary<string, string>();
        }
        catch (Exception ex)
        {
            throw new Exception($"将文件解析为配置文件失败：{_filePath}", ex);
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
            try
            {
                _saveEvent.Reset();
                string serializedContent = _serializer.Serialize(_content);
                var tmpFile = _filePath + ".tmp";
                File.WriteAllText(tmpFile, serializedContent);
                File.Replace(tmpFile, _filePath, null);
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Setup", "向硬盘同步配置文件失败：" + _filePath);
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
        _cts.Dispose();
        _saveEvent.Dispose();
        _saveTask.Dispose();
    }
}