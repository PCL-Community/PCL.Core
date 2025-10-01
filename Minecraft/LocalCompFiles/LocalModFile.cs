using PCL.Core.Minecraft.LocalCompFiles.Models;
using PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalModFile : LocalResource
{
    public ModMetadata? Metadata { get; set; }

    public Dictionary<string, string?> Dependencies { get; } = new();

    private static readonly List<IModMetadataParser> _Parsers =
    [
        new LegacyForgeModParser(),
        new ForgeModParser(),
        new FabricModJsonParser(),
        new QuitModParser(),
        new PackPngParser()
    ];

    /// <inheritdoc />
    public LocalModFile(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override BaseResourceData? Load(bool lazy = false)
    {
        if (File.Exists(Path))
        {
            FileUnavailableReason = new FileNotFoundException("Resource file not found.", Path);
            State = FileStatus.Unavailable;
            return null;
        }

        if (Path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) ||
            Path.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
        {
            State = FileStatus.Disabled;
        }

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(Path);
            foreach (var parser in _Parsers)
            {
                parser.TryParse(archive, this);
            }
        }
        catch (Exception ex)
        {
            FileUnavailableReason = ex;
            State = FileStatus.Unavailable;
        }

        if (string.IsNullOrEmpty(Metadata.Name))
        {
            Metadata = Metadata with { Name = System.IO.Path.GetFileNameWithoutExtension(RawFileName) };
        }

        return null;
    }
}