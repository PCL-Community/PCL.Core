using System.IO.Compression;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public class PackPngParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        var entry = archive.GetEntry("pack.png");
        if (entry == null)
        {
            return false;
        }

        var logoPath = ParserHelper.ExtractLogo(archive, "pack.png", modFile.Metadata.Icon) ?? string.Empty;

        modFile.Metadata = modFile.Metadata with { Icon = logoPath };

        return string.IsNullOrEmpty(logoPath);
    }
}