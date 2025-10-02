using System.IO.Compression;
using PCL.Core.Minecraft.Compoment.LocalComp;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public interface IModMetadataParser
{
    bool TryParse(ZipArchive archive, LocalModFile modFile);
}