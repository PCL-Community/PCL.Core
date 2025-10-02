namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record SpongeSchemaData(
    string Name,
    string Author,
    int RegionCount,
    int TotalVolume,
    int Version,
    int DataVersion,
    EnclosingSizeData EnclosingSize) : BaseNbtData(Name, TotalVolume, EnclosingSize);