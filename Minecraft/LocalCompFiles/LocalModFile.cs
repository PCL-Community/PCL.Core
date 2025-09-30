using PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalModFile : LocalResource
{
    public ModMetadata Metadata { get; set; }
    public Dictionary<string, string?> Dependencies { get; } = new();

    private static readonly List<IModMetadataParser> Parsers =
    [
        new FabricModJsonParser(),
        new ForgeModParser(),
        new LegacyForgeParser(),
        new PackPngParser()
    ];

    /// <inheritdoc />
    public LocalModFile(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override void Load()
    {
        if (File.Exists(Path))
        {
            FileUnavailableReason = new FileNotFoundException("Resource file not found.", Path);
            State = FileStatus.Unavailable;
            return;
        }

        if (Path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) ||
            Path.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
        {
            State = FileStatus.Disabled;
        }

        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(Path);
            foreach (var parser in Parsers)
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
    }
}