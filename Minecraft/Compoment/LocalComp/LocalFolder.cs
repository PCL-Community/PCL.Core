using System.IO;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalFolder : LocalResource
{
    /// <inheritdoc />
    public LocalFolder(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override BaseResourceData? Load(bool lazy = false)
    {
        if (Directory.Exists(ActualPath))
        {
            FileUnavailableReason = new DirectoryNotFoundException($"Directory '{ActualPath}' not found.");
            State = FileStatus.Unavailable;
        }

        return null;
    }
}