using System;
using System.Dynamic;
using System.IO;
using fNbt;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalSchematicFile : LocalResource
{
    public string? EnclosingSize { get; private set; }
    public int? TotalBlocks { get; private set; }
    public int? TotalVolume { get; private set; }
    public string? OriginalAuthor { get; private set; }

    /// <inheritdoc />
    public LocalSchematicFile(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override void Load()
    {
        if (!File.Exists(Path))
        {
            FileUnavailableReason = new FileNotFoundException("Resource file not found.", Path);
            State = FileStatus.Unavailable;
            return;
        }

        try
        {
            using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var file = new NbtFile();
            file.LoadFromStream(fs, NbtCompression.AutoDetect);

            var extension = System.IO.Path.GetExtension(Path).ToLowerInvariant();

            switch (extension) // TODO: impl parsing logic
            {
                case ".litematic":
                    break;
                case ".schem":
                    break;
                case ".schematic":
                    break;
                case ".nbt":
                    break;
            }
        }
        catch (Exception ex)
        {
            FileUnavailableReason = ex;
            State = FileStatus.Unavailable;
        }
    }

    private void LoadLitematicNbtData(NbtFile file)
    {
        var versionTag = file.RootTag.Get("Version");
        // TODO: impl this
    }
}