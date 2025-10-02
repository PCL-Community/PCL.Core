namespace PCL.Core.Minecraft.Compoment.LocalComp.Entities;

public record EnclosingSizeData(int X, int Y, int Z)
{
    public static readonly EnclosingSizeData Zero = new(0, 0, 0);

    /// <inheritdoc />
    public override string ToString() => $"{X} x {Y} x {Z}";
};