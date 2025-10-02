using System.IO.Compression;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

internal class FabricModJsonParser : IModMetadataParser
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
            var dto = JsonSerializer.Deserialize<FabricMetadataDto>(content);
            if (dto == null)
            {
                return false;
            }

            var metadata = new ModMetadata
            {
                Name = dto.Name,
                Id = dto.Id,
                Authors = dto.Authors ?? [],
                Description = dto.Description,
                Icon = dto.Icon,
                Version = dto.Version,
                Url = dto.Contact?.Homepage ?? string.Empty
            };

            // NOTE: fabric desnt parse dependencies

            modFile.Metadata = metadata;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}