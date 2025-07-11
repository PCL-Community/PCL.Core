using System;

namespace PCL.Core.Model.Files;

/// <summary>
/// Transfer a <see cref="FileItem"/> from predefined <see cref="FileItem.Sources"/>
/// to <see cref="FileItem.TargetPath"/>, return the real path by <paramref name="resultCallback"/>,
/// or <c>null</c> if transfer has failed
/// </summary>
public delegate void FileTransfer(FileItem item, Action<string?> resultCallback);
