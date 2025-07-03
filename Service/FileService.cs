using System;
using System.Collections.Concurrent;
using System.IO;
using PCL.Core.LifecycleManagement;
using PCL.Core.Utils;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading, Priority = 10000)]
public sealed class FileService : ILifecycleService
{
    private static LifecycleContext? _context = null;
    private static ConcurrentDictionary<string, IFileOwner>? _activeFiles = null;

    private FileService()
    {
        _context = Lifecycle.GetContext(this);
    }

    public string Identifier => "file-manage";
    public string Name => "文件资源管理";
    public bool SupportAsyncStart => true;

    private static LifecycleContext Context =>
        _context ?? throw new ObjectDisposedException("服务未启动");

    private static ConcurrentDictionary<string, IFileOwner> ActiveFiles =>
        _activeFiles ?? throw new ObjectDisposedException("服务已停止");

    public void Start() { }

    public void Stop()
    {
        foreach (var pair in ActiveFiles)
        {
            Context.Trace("强制释放文件：" + pair.Key);
            pair.Value.ForceReleaseFile();
        }
        foreach (var pair in _activeFiles)
            Context.Warn("文件释放失败：" + pair.Key);
        _activeFiles = null!;
    }

    public static FileHandle Open(
        string filePath,
        IFileOwner owner,
        FileMode mode = FileMode.OpenOrCreate,
        FileAccess access = FileAccess.Read,
        FileShare share = FileShare.Read)
    {
        if (filePath is null)
            throw new ArgumentNullException(nameof(filePath));
        if (owner is null)
            throw new ArgumentNullException(nameof(owner));
        Context.Trace("打开文件：" + filePath);
        if (!ActiveFiles.TryAdd(filePath, owner))
            throw new InvalidOperationException("文件已被其他所有者打开");
        FileStream fs;
        try
        {
            if (Path.GetDirectoryName(filePath) is { Length: > 0 } dir)
                Directory.CreateDirectory(dir);
            fs = new FileStream(filePath, mode, access, share);
        }
        catch (Exception ex)
        {
            Context.Warn("文件打开失败：" + filePath);
            ActiveFiles.TryRemove(filePath, out _);
            throw new IOException("文件打开失败", ex);
        }
        return new FileHandle(filePath, fs, () =>
        {
            Context.Trace("释放文件：" + filePath);
            ActiveFiles.TryRemove(filePath, out _);
        });
    }
}

public interface IFileOwner
{
    void ForceReleaseFile();
}