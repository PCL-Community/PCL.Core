namespace PCL.Core.UI.Animation;

/// <summary>
/// 动画基础种类。
/// </summary>
public enum AniType
{
    /// <summary>
    /// 单个Double的动画，包括位置、长宽、透明度等。这需要附属类型。
    /// </summary>
    /// <remarks></remarks>
    Number,
    /// <summary>
    /// 颜色属性的动画。这需要附属类型。
    /// </summary>
    /// <remarks></remarks>
    Color,
    /// <summary>
    /// 缩放控件大小。比起4个DoubleAnimation来说效率更高。
    /// </summary>
    /// <remarks></remarks>
    Scale,
    /// <summary>
    /// 文字一个个出现。
    /// </summary>
    /// <remarks></remarks>
    TextAppear,
    /// <summary>
    /// 执行代码。
    /// </summary>
    /// <remarks></remarks>
    Code,
    /// <summary>
    /// 以 WPF 方式缩放控件。
    /// </summary>
    ScaleTransform,
    /// <summary>
    /// 以 WPF 方式旋转控件。
    /// </summary>
    RotateTransform
}

/// <summary>
/// 动画扩展种类。
/// </summary>
public enum AniTypeExt
{
    X,
    Y,
    Width,
    Height,
    Opacity,
    Value,
    Radius,
    BorderThickness,
    StrokeThickness,
    TranslateX,
    TranslateY,
    Double,
    DoubleParam,
    GridLengthWidth
}
