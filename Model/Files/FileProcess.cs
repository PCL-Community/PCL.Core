namespace PCL.Core.Model.Files;

/// <summary>
/// Process a loaded <see cref="FileItem"/>.
/// </summary>
/// <param name="item">the item to process</param>
/// <param name="path">the real path of the file, or <c>null</c> if the file is not found and fail to transfer</param>
/// <returns>process result</returns>
public delegate object? FileProcess(FileItem item, string? path);
