using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            string? metaModId = null;
            if (modTable.TryGetValue("modId", out var modId))
            {
                metaModId = modId.ToString();
            }

            string? metaVersion = null;
            if (modTable.TryGetValue("version", out var version))
            {
                metaVersion = version.ToString();
            }

            string? metaName = null;
            if (modTable.TryGetValue("displayName", out var name))
            {
                metaName = name.ToString();
            }

            string? metaDescription = null;
            if (modTable.TryGetValue("description", out var description))
            {
                metaDescription = description.ToString();
            }

            string? metaAuthors = null;
            if (modTable.TryGetValue("authors", out var authors))
            {
                metaAuthors = authors.ToString();
            }

            string? metaLogo = null;
            if (modTable.TryGetValue("authors", out var logo))
            {
                metaLogo = logo.ToString();
            }

            var metadata = new ModMetadata(metaName ?? string.Empty,
                metaDescription,
                metaVersion,
                metaModId ?? string.Empty,
                [metaAuthors ?? string.Empty],
                metaLogo ?? string.Empty);

            modFile.Metadata = metadata;

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}