using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using PCL.Core.Utils;

namespace PCL.Core.ProgramSetup.FileManager;

/// <summary>
/// 用于托管某个位于游戏实例文件夹内的配置文件的类，同步地写入文件
/// </summary>
public sealed class InstanceSetupFileManager : ISetupFileManager, IDisposable
{
    private readonly CountingDictionary<string, ConcurrentDictionary<string, string>> _activeFilesDict =
        new(Companion.LoadFile, Companion.WriteFile);

    private bool _disposed = false;

    public string? Get(string key, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        var filePath = GetSetupFilePath(mcPath);
        var content = _activeFilesDict.Acquire(filePath);
        var result = content.TryGetValue(key, out var value) ? value : null;
        _activeFilesDict.Release(filePath);
        return result;
    }

    public string? Set(string key, string value, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        var filePath = GetSetupFilePath(mcPath);
        var content = _activeFilesDict.Acquire(filePath);
        string? result = null;
        content.AddOrUpdate(key, _ =>
        {
            result = null;
            return value;
        }, (_, existingValue) =>
        {
            result = existingValue;
            return value;
        });
        _activeFilesDict.Release(filePath);
        return result;
    }

    public string? Remove(string key, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        var filePath = GetSetupFilePath(mcPath);
        var content = _activeFilesDict.Acquire(filePath);
        content.TryRemove(key, out var result);
        _activeFilesDict.Release(filePath);
        return result;
    }

    public MultipleOperationHandle BeginMultipleOperation(string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        var filePath = GetSetupFilePath(mcPath);
        _activeFilesDict.Acquire(filePath);
        return new MultipleOperationHandle(() => _activeFilesDict.Release(filePath));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _activeFilesDict.Dispose();
    }

    private static string GetSetupFilePath(string mcPath) => Path.Combine(mcPath, "PCL", "Setup.ini");
}

file static class Companion
{
    private static ISetupFileSerializer Serializer => SetupIniSerializer.Instance;

    public static ConcurrentDictionary<string, string> LoadFile(string filePath)
    {
        if (Path.GetDirectoryName(filePath) is { Length: > 0 } dir)
            Directory.CreateDirectory(dir);
        using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
        return Serializer.Deserialize(fs) ?? new ConcurrentDictionary<string, string>();
    }

    public static void WriteFile(string filePath, ConcurrentDictionary<string, string> content)
    {
        if (Path.GetDirectoryName(filePath) is { Length: > 0 } dir)
            Directory.CreateDirectory(dir);
        var serialized = Encoding.UTF8.GetBytes(Serializer.Serialize(content));
        using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        fs.Write(serialized, 0, serialized.Length);
    }
}