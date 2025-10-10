using System;

namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record LitematicNbtData(
    string Name,
    int Version,
    string Description,
    string Author,
    DateTime Created,
    DateTime Modified,
    EnclosingSizeData EnclosingSize,
    int RegionCount,
    int TotalBlocks,
    int TotalVolume) : BaseNbtData(Name, TotalVolume, EnclosingSize);