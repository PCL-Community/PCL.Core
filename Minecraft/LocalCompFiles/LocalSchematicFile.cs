using System;
using System.IO;
using System.Linq;
using fNbt;
using PCL.Core.Logging;
using PCL.Core.Minecraft.LocalCompFiles.Models;

namespace PCL.Core.Minecraft.LocalCompFiles;

public class LocalSchematicFile : LocalResource
{
    /// <inheritdoc />
    public LocalSchematicFile(string path) : base(path)
    {
    }

    /// <inheritdoc />
    public override BaseNbtData? Load()
    {
        if (!File.Exists(Path))
        {
            FileUnavailableReason = new FileNotFoundException("Resource file not found.", Path);
            State = FileStatus.Unavailable;
            return null;
        }

        try
        {
            using var fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var file = new NbtFile();
            file.LoadFromStream(fs, NbtCompression.AutoDetect);

            var extension = System.IO.Path.GetExtension(Path).ToLowerInvariant();

            BaseNbtData? nbtData = extension switch
            {
                ".litematic" => _LoadLitematicNbtData(file),
                ".schem" => _LoadSchemaNbtData(file),
                ".schematic" => _LoadSchematicNbtData(file),
                ".nbt" => _LoadStructureNbtData(file),
                _ => null
            };

            return nbtData;
        }
        catch (Exception ex)
        {
            FileUnavailableReason = ex;
            State = FileStatus.Unavailable;
        }

        return null;
    }

    private LitematicNbtData _LoadLitematicNbtData(NbtFile nbtFile)
    {
        var versionTag = nbtFile.RootTag.Get<NbtInt>("Version")?.Value ?? 0;

        var metadataTag = nbtFile.RootTag.Get<NbtCompound>("Metadata");
        if (metadataTag == null)
        {
            LogWrapper.Debug("Lotematic metadata node not found.");
            throw new ArgumentNullException(nameof(metadataTag), "Metadata node not found.");
        }

        var nameTag = metadataTag.Get<NbtString>("Name")?.Value ?? string.Empty;
        var descriptionTag = metadataTag.Get<NbtString>("Description")?.Value ?? string.Empty;
        var authorTag = metadataTag.Get<NbtString>("Author")?.Value ?? string.Empty;
        var timeCreatedTag = metadataTag.Get<NbtLong>("TimeCreated")?.Value ?? 0L;
        var timeModifiedTag = metadataTag.Get<NbtLong>("TimeModified")?.Value ?? 0L;

        var regionCountTag = metadataTag.Get<NbtInt>("RegionCount")?.Value ?? 0;
        var totalBlocksTag = metadataTag.Get<NbtInt>("TotalBlocks")?.Value ?? 0;
        var totalVloumeTag = metadataTag.Get<NbtInt>("TotalVolume")?.Value ?? 0;

        var convertedCreatedTime = DateTimeOffset.FromUnixTimeSeconds(timeCreatedTag).DateTime.ToLocalTime();
        var convertedmodifiedTime = DateTimeOffset.FromUnixTimeSeconds(timeModifiedTag).DateTime.ToLocalTime();

        var nbtData = new LitematicNbtData(nameTag, versionTag, descriptionTag, authorTag,
            convertedCreatedTime,
            convertedmodifiedTime, new EnclosingSizeData(0, 0, 0), regionCountTag, totalBlocksTag, totalVloumeTag);

        var enclosingSizeTag = metadataTag.Get<NbtCompound>("EnclosingSizeData");

        if (enclosingSizeTag == null) return nbtData;

        var xTag = enclosingSizeTag.Get<NbtInt>("x")?.Value ?? 0;
        var yTag = enclosingSizeTag.Get<NbtInt>("y")?.Value ?? 0;
        var zTag = enclosingSizeTag.Get<NbtInt>("z")?.Value ?? 0;

        nbtData = nbtData with { EnclosingSize = new EnclosingSizeData(xTag, yTag, zTag) };

        return nbtData;
    }

    private SchemaData _LoadSchemaNbtData(NbtFile nbtFile)
    {
        var versionTag = nbtFile.RootTag.Get<NbtInt>("Version")?.Value ?? 0;
        var dataVersionTag = nbtFile.RootTag.Get<NbtInt>("DataVersion")?.Value ?? 0;
        var widhTag = nbtFile.RootTag.Get<NbtShort>("Width")?.Value ?? 0;
        var heightTag = nbtFile.RootTag.Get<NbtShort>("Height")?.Value ?? 0;
        var lengthTag = nbtFile.RootTag.Get<NbtShort>("Length")?.Value ?? 0;

        var enclosingSize = new EnclosingSizeData(widhTag, heightTag, lengthTag);
        var totalVolume = widhTag * heightTag * lengthTag;

        var paletteTag = nbtFile.RootTag.Get<NbtCompound>("Palette");
        var regionCount = paletteTag is null ? 0 : 1; // sponge schematic only have one region

        var metadataTag = nbtFile.RootTag.Get<NbtCompound>("Metadata");

        var nameTag = metadataTag?.Get<NbtString>("Name")?.Value ?? string.Empty;
        var authorTag = metadataTag?.Get<NbtString>("Author")?.Value ?? string.Empty;

        var schemaData = new SchemaData(nameTag, authorTag, regionCount, totalVolume, versionTag, dataVersionTag,
            enclosingSize);

        return schemaData;
    }

    private SchematicData _LoadSchematicNbtData(NbtFile nbtFile)
    {
        var widhTag = nbtFile.RootTag.Get<NbtShort>("Width")?.Value ?? 0;
        var heightTag = nbtFile.RootTag.Get<NbtShort>("Height")?.Value ?? 0;
        var lengthTag = nbtFile.RootTag.Get<NbtShort>("Length")?.Value ?? 0;

        var enclosingSize = new EnclosingSizeData(widhTag, heightTag, lengthTag);
        var totalVolume = widhTag * heightTag * lengthTag;

#if DEBUG
        var materialsTag = nbtFile.RootTag.Get<NbtString>("Materials")?.Value ?? string.Empty;

        LogWrapper.Debug($"Schematic material type: {materialsTag}");
#endif

        var schematicData = new SchematicData(totalVolume, enclosingSize);

        return schematicData;
    }

    private VanillaNbtData _LoadStructureNbtData(NbtFile nbtFile)
    {
        var authorTag = nbtFile.RootTag.Get<NbtString>("author")?.Value ?? string.Empty;
        var sizeElements = nbtFile.RootTag.Get<NbtList>("size")?.ToArray() ?? [];

        var enclosingSize = EnclosingSizeData.Zero;
        var totalVolume = 0;
        if (sizeElements.Length >= 3)
        {
            var sizeArray = sizeElements.Take(3).Select(it => it.IntValue).ToArray();
            enclosingSize = new EnclosingSizeData(sizeArray[0], sizeArray[1], sizeArray[2]);
            totalVolume = sizeArray[0] * sizeArray[1] * sizeArray[2];
        }

        var blocksCount = nbtFile.RootTag.Get<NbtList>("blocks")?.Count(it => it.TagType is NbtTagType.Compound) ?? 0;

        var regionCount = nbtFile.RootTag.Get<NbtList>("palette") is null ? 0 : 1;

        var vanillaNbtData = new VanillaNbtData(authorTag, enclosingSize, totalVolume, blocksCount, regionCount);

        return vanillaNbtData;
    }
}