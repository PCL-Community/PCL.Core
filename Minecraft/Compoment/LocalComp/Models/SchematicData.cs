namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record SchematicData(string Name, int TotalVolume, EnclosingSizeData EnclosingSize)
    : BaseNbtData(Name, TotalVolume, EnclosingSize);