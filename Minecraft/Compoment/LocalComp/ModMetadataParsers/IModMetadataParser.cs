using System.IO.Compression;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public interface IModMetadataParser
{
    bool TryParse(ZipArchive archive, LocalModFile modFile);
}