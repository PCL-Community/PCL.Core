﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using PCL.Core.Helper;
using PCL.Core.Service;

namespace PCL.Core.Model.Files;

public enum FileType
{
    /// <summary>
    /// Not a special file.<br/>
    /// Storage path relative to <see cref="FileService.DefaultDirectory"/>.
    /// </summary>
    Plain,
    /// <summary>
    /// Common data: per-instance config JSON, game instance database, etc.<br/>
    /// Storage path relative to <see cref="FileService.DataPath"/>.
    /// </summary>
    Data,
    /// <summary>
    /// Shared data among all launcher instances: shared config JSON, global database, etc.<br/>
    /// Storage path relative to <see cref="FileService.SharedDataPath"/>.
    /// </summary>
    SharedData,
    /// <summary>
    /// File that can be restored anytime: help files, tools downloaded from the web, etc.<br/>
    /// Storage path relative to <see cref="FileService.LocalDataPath"/>.
    /// </summary>
    LocalData,
    /// <summary>
    /// Temporary file.<br/>
    /// Storage path relative to <see cref="FileService.TempPath"/>.
    /// </summary>
    Temporary,
}

/// <summary>
/// File process unit.
/// </summary>
/// <param name="Name">The file name, including extension.</param>
/// <param name="Type">The special type of the file, or <see cref="FileType.Plain"/> as the default value.</param>
/// <param name="Sources">Transfer sources, such as a web URL or a local path.</param>
public record FileItem(
    string Name,
    FileType Type = FileType.Plain,
    IEnumerable<string>? Sources = null)
{
    
    private string? _targetDirectory;

    /// <summary>
    /// The parent directory path of the file. Special types should keep <c>null</c> value.
    /// </summary>
    public string? TargetDirectory
    {
        get => _targetDirectory;
        set => _targetDirectory = (value == null) ? null : Path.GetFullPath(Path.Combine(FileService.DefaultDirectory, value));
    }
    
    private string? _targetPath;

    /// <summary>
    /// The path to storage the file.<br/>
    /// If <see cref="TargetDirectory"/> has been set, return the path relative to it,
    /// otherwise return a combined path by <see cref="Type"/> and <see cref="Name"/>
    /// </summary>
    public string TargetPath
    {
        get
        {
            if (TargetDirectory != null) return Path.Combine(TargetDirectory, Name);
            if (_targetPath != null) return _targetPath;
            var directory = Type switch
            {
                // special types
                FileType.Data => FileService.DataPath,
                FileType.LocalData => FileService.LocalDataPath,
                FileType.SharedData => FileService.SharedDataPath,
                FileType.Temporary => FileService.TempPath,
                // other types: relative to default
                _ => FileService.DefaultDirectory
            };
            var value = Path.Combine(directory, Name);
            _targetPath = value;
            return value;
        }
    }
    
    public override int GetHashCode() => TargetPath.GetHashCode();

    public override string ToString()
    {
        var str = TargetPath;
        if (Sources != null) str += $" [{string.Join(", ", Sources)}]";
        return str;
    }
}
