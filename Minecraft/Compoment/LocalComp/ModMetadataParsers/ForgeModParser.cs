using System;
using System.IO.Compression;
using System.Linq;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using Tomlyn;

namespace PCL.Core.Minecraft.Compoment.LocalComp.ModMetadataParsers;

internal class ForgeModParser : IModMetadataParser
{
    /// <inheritdoc />
    public bool TryParse(ZipArchive archive, LocalModFile modFile)
    {
        var entry = archive.GetEntry("META-INF/mods.toml");
        var content = ParserHelper.ReadEntryContent(entry);

        if (content == null)
        {
            return false;
        }

        try
        {
            var model = Toml.ToModel<ForgeMetadataDto>(content);
            var modInfo = model.Mods.FirstOrDefault() ??
                          throw new ArgumentNullException(nameof(model), "Mod info is null.");

            foreach (var dep in model.Dependencies.Select(dep => dep.Value))
            {
                foreach (var info in dep)
                {
                    var modId = info.ModId;
                    var reqVers = info.VersionRange;

                    modFile.AddDependency(modId, reqVers);
                }
            }

            var metadata = new ModMetadata
            {
                Name = modInfo.DisplayName,
                Description = modInfo.Description,
                Version = modInfo.Version,
                Id = modInfo.ModId,
                Authors = [modInfo.Authors],
                Icon = modInfo.LofoFile,
                Url = modInfo.Url
            };

            modFile.Metadata = metadata;

            return true;
        }
        catch (Exception)
        {
            // ignore exception
            return false;
        }
    }
}