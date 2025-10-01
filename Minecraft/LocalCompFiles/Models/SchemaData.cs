namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record SchemaData(
    string Name,
    string Author,
    int RegionCount,
    int TotalVolume,
    int Version,
    int DataVersion,
    EnclosingSizeData EnclosingSize) : BaseNbtData(TotalVolume, EnclosingSize);