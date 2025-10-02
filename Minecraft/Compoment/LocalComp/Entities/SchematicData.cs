namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record SchematicData(string Name, int TotalVolume, EnclosingSizeData EnclosingSize)
    : BaseNbtData(Name, TotalVolume, EnclosingSize);