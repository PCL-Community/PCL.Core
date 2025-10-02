using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

internal class LegacyForgeModParser : IModMetadataParser
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
            var dto = JsonSerializer.Deserialize<LegacyForgeModMetadataDto>(content);
            if (dto == null)
            {
                return false;
            }

            var deps = (dto.Dependencies ?? []).Union(dto.RequireMods ?? []);
            foreach (var dep in deps)
            {
                if (string.IsNullOrEmpty(dep))
                {
                    continue;
                }

                if (dep.Contains('@'))
                {
                    var spilited = dep.Split('@');
                    var id = spilited[0];
                    var verReq = spilited[1];

                    modFile.AddDependency(id, verReq);
                }
                else
                {
                    modFile.AddDependency(dep);
                }
            }


            var metadata = new ModMetadata
            {
                Name = dto.Name,
                Description = dto.Description,
                Version = dto.Version,
                Id = dto.Id,
                Authors = dto.Authors,
                Icon = dto.LogoFile ?? string.Empty,
                Url = dto.Url
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