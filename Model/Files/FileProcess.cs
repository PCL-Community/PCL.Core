﻿using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Model.Files;

/// <summary>
/// Process a loaded <see cref="FileItem"/>.
/// </summary>
/// <param name="item">the item to process</param>
/// <param name="path">the real path of the file, or <c>null</c> if the file is not found and fail to transfer</param>
/// <returns>process result</returns>
public delegate object? FileProcess(FileItem item, string? path);

public static class FileProcesses
{
    /// <summary>
    /// Result: text content of the file.
    /// </summary>
    public static readonly FileProcess ReadText = ((_, path) =>
    {
        if (path == null) return null;
        var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
        var sr = new StreamReader(fs);
        return sr.ReadToEnd();
    });

    private static readonly JsonSerializerOptions _ParseJsonCreateDefaultOptions = new()
    {
        WriteIndented = true,
        AllowOutOfOrderMetadataProperties = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
    
    /// <summary>
    /// Result: parsed object as the specified type (nullable)
    /// </summary>
    /// <typeparam name="TValue">the specified type (must be serializable and have a public default constructor)</typeparam>
    /// <param name="createDefault">whether create the file with default values if not exist</param>
    public static FileProcess ParseJson<TValue>(bool createDefault = true) => ((_, path) =>
    {
        if (path == null) return null;
        var exist = File.Exists(path);
        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        if (exist)
        {
            var result = JsonSerializer.Deserialize<TValue>(fs);
            return result;
        }
        if (!createDefault) return null;
        var d = Activator.CreateInstance(typeof(TValue));
        JsonSerializer.Serialize(fs, d, _ParseJsonCreateDefaultOptions);
        return d;
    });
}
