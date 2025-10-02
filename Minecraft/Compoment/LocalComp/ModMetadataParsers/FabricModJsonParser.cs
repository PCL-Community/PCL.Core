using System.IO.Compression;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

public class FabricModJsonParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        var entry = archive.GetEntry("fabric.mod.json");
        var content = ParserHelper.ReadEntryContent(entry);

        if (content is null)
        {
            return false;
        }

        try
        {
            var jsonStu = JsonSerializer.Deserialize<ModMetadata>(content);
            if (jsonStu == null)
            {
                return false;
            }

            modFile.Metadata = jsonStu;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}