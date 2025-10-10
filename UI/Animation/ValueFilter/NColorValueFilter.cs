namespace PCL.Core.UI.Animation.ValueFilter;

public class NColorValueFilter : IValueFilter<NColor>
{
    public NColor Filter(NColor value)
    {
        if (value.A < 0) value.A = 0;
        if (value.R < 0) value.R = 0;
        if (value.G < 0) value.G = 0;
        if (value.B < 0) value.B = 0;
        
        return value;
    }
}