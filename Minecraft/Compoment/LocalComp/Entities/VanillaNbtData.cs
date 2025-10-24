namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record VanillaNbtData(
    string Author,
    EnclosingSizeData EnclosingSize,
    int TotalVolume,
    int BlockCount,
    int RegionCount) : BaseNbtData(string.Empty, TotalVolume, EnclosingSize);