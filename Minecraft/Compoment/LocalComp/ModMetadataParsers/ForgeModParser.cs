using System;
using System.IO.Compression;
using System.Linq;
using PCL.Core.Minecraft.Compoment.LocalComp;
using PCL.Core.Minecraft.Compoment.LocalComp.Entities;
using Tomlyn;
using Tomlyn.Model;

namespace PCL.Core.Minecraft.LocalCompFiles.ModMetadataParsers;

public class ForgeModParser : IModMetadataParser
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
            var model = Toml.ToModel(content);
            if (!model.TryGetValue("mods", out var modsEntry) ||
                modsEntry is not TomlTableArray modsArray)
            {
                return false;
            }

            var modTable = modsArray.FirstOrDefault();
            if (modTable == null)
            {
                return false;
            }

            var metaModId = string.Empty;
            if (modTable.TryGetValue("modId", out var modId))
            {
                metaModId = modId.ToString() ?? string.Empty;
            }

            var metaVersion = string.Empty;
            if (modTable.TryGetValue("version", out var version))
            {
                metaVersion = version.ToString() ?? string.Empty;
            }

            var metaName = string.Empty;
            if (modTable.TryGetValue("displayName", out var name))
            {
                metaName = name.ToString() ?? string.Empty;
            }

            var metaDescription = string.Empty;
            if (modTable.TryGetValue("description", out var description))
            {
                metaDescription = description.ToString() ?? string.Empty;
            }

            var metaAuthors = string.Empty;
            if (modTable.TryGetValue("authors", out var authors))
            {
                metaAuthors = authors.ToString() ?? string.Empty;
            }

            var metaLogo = string.Empty;
            if (modTable.TryGetValue("authors", out var logo))
            {
                metaLogo = logo.ToString() ?? string.Empty;
            }

            var metadata = new ModMetadata(metaName,
                metaDescription,
                metaVersion,
                metaModId,
                [metaAuthors],
                metaLogo);

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