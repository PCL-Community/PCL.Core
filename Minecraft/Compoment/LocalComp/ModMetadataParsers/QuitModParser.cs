using System.IO.Compression;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

internal class QuitModParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        var entry = archive.GetEntry("quilt.mod.json");
        var content = ParserHelper.ReadEntryContent(entry);

        if (content is null)
        {
            return false;
        }

        if (!content.Contains("schema_version"))
        {
            return false;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<QuiltMetadataDto>(content);
            if (dto == null)
            {
                return false;
            }

            var metadata = new ModMetadata
            {
                Name = dto.Loader.Metadata.Name,
                Id = dto.Loader.Id,
                Authors = [],
                Description = dto.Loader.Metadata.Description,
                Icon = dto.Loader.Icon ?? string.Empty,
                Version = dto.Loader.Version ?? string.Empty,
                Url = dto.Loader.Metadata.Contact?.Homepage ?? string.Empty
            };

            // NOTE: quilt desnt parse dependencies

            modFile.Metadata = metadata;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}