namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record SchematicData(int TotalVolume, EnclosingSizeData EnclosingSize) : BaseNbtData(TotalVolume, EnclosingSize);