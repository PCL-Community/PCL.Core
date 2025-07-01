using System;
using System.Collections.Concurrent;
using PCL.Core.LifecycleManagement;
using PCL.Core.Utils.FileResource;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading, Priority = 10000)]
public class FileManageService : ILifecycleService
{
    private readonly LifecycleContext _context;
    private ConcurrentDictionary<string, IFileOwner> _activeFiles = [];

    private FileManageService()
    {
        _context = Lifecycle.GetContext(this);
    }

    public string Identifier => "file-manage";
    public string Name => "文件资源管理";
    public bool SupportAsyncStart => true;

    public void Start() { }

    public void Stop()
    {
        foreach (var pair in _activeFiles)
        {
            _context.Trace("强制释放文件：" + pair.Key);
            pair.Value.ForceReleaseFile();
        }
        foreach (var pair in _activeFiles)
            _context.Warn("文件释放失败：" + pair.Key);
        _activeFiles = null!;
    }

    /// <summary>
    /// 打开一个自动托管文件，可通过返回的 <see cref="AutoManagedFileHandle"/> 操作文件内容
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="owner">文件所有者，用于请求强制释放文件</param>
    /// <returns>打开的 <see cref="AutoManagedFileHandle"/></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="filePath"/> 或 <paramref name="owner"/>为 <see langword="null"/>
    /// </exception>
    /// <exception cref="InvalidOperationException">该文件已被打开</exception>
    public AutoManagedFileHandle OpenAutoManagedFile(string filePath, IFileOwner owner)
    {
        _context.Trace("打开托管文件：" + filePath);
        if (!_activeFiles.TryAdd(
                filePath ?? throw new ArgumentNullException(nameof(filePath)),
                owner ?? throw new ArgumentNullException(nameof(owner))))
            throw new InvalidOperationException("该文件已被托管至另一个所有者");
        var result = new AutoManagedFileHandle(
            filePath,
            p =>
            {
                _context.Trace("托管文件被释放：" + filePath);
                _activeFiles.TryRemove(p, out _);
            });
        return result;
    }
}