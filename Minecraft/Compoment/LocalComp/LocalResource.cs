using System;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.Compoment.LocalComp;

public abstract class LocalResource(string path)
{
    /// <summary>
    /// Absolute path of resource.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Folder name or File name of resource.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(ActualPath);

    public virtual string RawFileName => FileName.Replace(".disabled", string.Empty).Replace(".old", string.Empty);

    /// <summary>
    /// Demonstrate if the resource is a folder.
    /// </summary>
    public bool IsFolder { get; } = path?.EndsWith("\\__FOLDER__", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Actual path of resource, if it's a folder, the path will remove the "__FOLDER__" suffix.
    /// </summary>
    public string ActualPath => IsFolder ? Path.Replace("\\__FOLDER__", string.Empty) : Path;

    public FileStatus State { get; protected set; } = FileStatus.Fine;
    public Exception? FileUnavailableReason { get; protected set; }
    public bool IsFileAvailable => FileUnavailableReason is null;

    /// <summary>
    /// Load and parse the resource from disk.
    /// </summary>
    public abstract BaseResourceData? Load(bool lazy = false);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{State} - {Path}";
    }


    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is LocalResource other && Path.Equals(other.Path, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }
}