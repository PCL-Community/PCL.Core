using System;
using System.Collections.Generic;
using System.IO;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;
using PCL.Core.Minecraft.LocalCompFiles;
using PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

namespace PCL.Core.Minecraft.Compoment.LocalComp;

public class LocalModFile : LocalResource
{
    public ModMetadata? Metadata { get; set; }

    public Dictionary<string, string?> Dependencies { get; } = new(); // TODO: impl dependencies parsing

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
    /// <exception cref="ArgumentNullException">Throw if Metadata is null after parsing</exception>
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

        ArgumentNullException.ThrowIfNull(Metadata);

        if (string.IsNullOrEmpty(Metadata.Name))
        {
            Metadata = Metadata with { Name = System.IO.Path.GetFileNameWithoutExtension(RawFileName) };
        }

        return null;
    }
}