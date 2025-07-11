using System.Collections.Generic;

namespace PCL.Core.Model.Files;

public delegate void FileProcessFinishedEvent(FileItem item, object? result);

public interface IFileTask
{
    public IEnumerable<FileItem> Items { get; }

    public FileTransfer? GetTransfer(FileItem item);
    
    public FileProcess? GetProcess(FileItem item);

    public bool OnProcessFinished(FileItem item, object? result);
    
    public void OnTaskFinished(object? result);
}
