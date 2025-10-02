using System.IO.Compression;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.LocalComp;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public class LegacyForgeModParser : IModMetadataParser
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
            var jsonData = JsonSerializer.Deserialize<LegacyForgeModMetadata>(content);
            if (jsonData == null)
            {
                return false;
            }

            var metadata = new ModMetadata
            {
                Name = jsonData.Name,
                Description = jsonData.Description,
                Version = jsonData.Version,
                Id = jsonData.Id,
                Authors = jsonData.Authors,
                Icon = jsonData.LogoFile ?? string.Empty
            };

            modFile.Metadata = metadata;

            return true;
        }
        catch (JsonException)
        {
            // ignore exception
            return false;
        }
    }
}