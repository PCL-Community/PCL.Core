using System.IO.Compression;
using PCL.Core.Minecraft.Compoment.LocalComp;

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