using System;
using System.IO;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalFolder : LocalResource
{
    /// <inheritdoc />
    public LocalFolder(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override void Load()
    {
        if (Directory.Exists(ActualPath))
        {
            FileUnavailableReason = new DirectoryNotFoundException($"Directory '{ActualPath}' not found.");
            State = FileStatus.Unavailable;
        }
    }
}