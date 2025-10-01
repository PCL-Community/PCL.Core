namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record SpongeSchemaData(
    string Name,
    string Author,
    int RegionCount,
    int TotalVolume,
    int Version,
    int DataVersion,
    EnclosingSizeData EnclosingSize) : BaseNbtData(Name, TotalVolume, EnclosingSize);