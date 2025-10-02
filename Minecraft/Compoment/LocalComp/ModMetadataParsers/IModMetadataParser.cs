using System.IO.Compression;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

public interface IModMetadataParser
{
    bool TryParse(ZipArchive archive, LocalModFile modFile);
}