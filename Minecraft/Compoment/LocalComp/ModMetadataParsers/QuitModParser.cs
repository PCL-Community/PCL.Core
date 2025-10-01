using System.IO.Compression;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public class QuitModParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        // TODO: impl this parser
        return false;
    }
}