namespace PCL.Core.UI.Animation.ValueProcessor;

public class NRotateTransformValueProcessor : IValueProcessor<NRotateTransform>
{
    public NRotateTransform Filter(NRotateTransform value) => value;
    
    public NRotateTransform Add(NRotateTransform value1, NRotateTransform value2) => value1 + value2;

    public NRotateTransform Subtract(NRotateTransform value1, NRotateTransform value2) => value1 - value2;
    public NRotateTransform Scale(NRotateTransform value, double factor) => value * (float)factor;
    public NRotateTransform DefaultValue() => new();
}