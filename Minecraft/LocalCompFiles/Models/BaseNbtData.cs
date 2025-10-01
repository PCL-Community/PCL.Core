namespace PCL.Core.Minecraft.LocalCompFiles.Models;

public record BaseNbtData(string Name, int TotalVolume, EnclosingSizeData EnclosingSize) : BaseResourceData;