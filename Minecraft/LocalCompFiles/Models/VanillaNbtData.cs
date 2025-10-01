namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record VanillaNbtData(
    string Author,
    EnclosingSizeData EnclosingSize,
    int TotalVolume,
    int BlockCount,
    int RegionCount) : BaseNbtData(TotalVolume, EnclosingSize);