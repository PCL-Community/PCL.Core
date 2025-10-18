namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record BaseNbtData(string Name, int TotalVolume, EnclosingSizeData EnclosingSize) : BaseResourceData;