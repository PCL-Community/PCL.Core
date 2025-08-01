using System.Collections.Generic;

namespace PCL.Core.Model.Files;

public interface IFileTask
{
    public IEnumerable<FileItem> Items { get; }

    public IEnumerable<FileTransfer> GetTransfer(FileItem item);
    
    public FileProcess? GetProcess(FileItem item);

    public bool OnProcessFinished(FileItem item, object? result);
    
    public void OnTaskFinished(object? result);
}
