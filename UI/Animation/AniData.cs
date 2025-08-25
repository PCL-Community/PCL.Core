using System;

namespace PCL.Core.UI.Animation;

/// <summary>
/// 单个动画对象。
/// </summary>
/// <remarks></remarks>
public struct AniData
{

    /// <summary>
    /// 动画种类。
    /// </summary>
    /// <remarks></remarks>
    public AniType TypeMain;
    /// <summary>
    /// 动画副种类。
    /// </summary>
    /// <remarks></remarks>
    public AniTypeExt TypeSub;

    /// <summary>
    /// 动画总长度。
    /// </summary>
    /// <remarks></remarks>
    public int TimeTotal;
    /// <summary>
    /// 已经执行的动画长度。如果为负数则为延迟。
    /// </summary>
    /// <remarks></remarks>
    public int TimeFinished;
    /// <summary>
    /// 已经完成的百分比。
    /// </summary>
    /// <remarks></remarks>
    public double TimePercent;

    /// <summary>
    /// 是否为“以后”。
    /// </summary>
    /// <remarks></remarks>
    public bool IsAfter;

    /// <summary>
    /// 插值器类型。
    /// </summary>
    /// <remarks></remarks>
    public AniEase Ease;
    /// <summary>
    /// 动画对象。
    /// </summary>
    /// <remarks></remarks>
    public object Obj;
    /// <summary>
    /// 动画值。
    /// </summary>
    /// <remarks></remarks>
    public object Value;
    /// <summary>
    /// 上次执行时的动画值。
    /// </summary>
    /// <remarks></remarks>
    public object ValueLast;

    public override readonly string ToString()
    {
        Enum enumData = (Enum)TypeMain;
        return Enum.GetName(enumData.GetType(), enumData) + " | " + TimeFinished + "/" + TimeTotal + "(" + Math.Round(TimePercent * 100d) + "%)" + (Obj is null ? "" : " | " + Obj.ToString() + "(" + Obj.GetType().Name + ")");
    }
}