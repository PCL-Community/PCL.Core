using System;
using System.IO;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalResourceFactory
{
    public static LocalResource Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
        }

        if (path.EndsWith("\\__FOLDER__", StringComparison.OrdinalIgnoreCase))
        {
            return new LocalFolder(path);
        }

        //if (Directory.Exists(path))
        //{
        //    return new LocalFolder(path);
        //}


        //var extension = Path.GetExtension(path).ToLowerInvariant();
        var rawExtension = Path.GetExtension(path)
            .Replace(".disabled", string.Empty)
            .Replace(".old", string.Empty)
            .ToLowerInvariant();

        return rawExtension switch
        {
            ".jar" or ".zip" or ".litemod" => new LocalModFile(path),
            ".litematic" or ".schem" or ".schematic" or ".nbt" => new LocalSchematicFile(path),
            _ => throw new NotSupportedException($"File type not supported: '{path}'")
        };
    }
}