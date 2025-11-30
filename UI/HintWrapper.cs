namespace PCL.Core.UI;

/// <summary>
/// 提示信息的种类W
/// </summary>
public enum HintType
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,
    /// <summary>
    /// 已完成
    /// </summary>
    Finish,
    /// <summary>
    /// 错误
    /// </summary>
    Critical
}

public delegate void HintHandler(
    string message,
    HintType type
);

public static class HintWrapper
{
    public static event HintHandler? OnShow;

    public static void Show(string message, HintType type = HintType.Info)
    {
        OnShow?.Invoke(message, type);
    }
}
