using System.IO.Compression;
using System.Text.Json;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public class LegacyForgeParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        var entry = archive.GetEntry("mcmod.info");
        var content = ParserHelper.ReadEntryContent(entry);

        if (content == null)
        {
            return false;
        }

        try
        {
            var token = JsonDocument.Parse(content);
            var modInfo = token.RootElement;

            // WARN: i dont know how to parse legacy forge mod metadata... /cc whitecat346

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}