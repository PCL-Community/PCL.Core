using System;

namespace PCL.Core.UI;

[Obsolete("请使用 HintType 而不是 HintTheme")]
public enum HintTheme
{
    Normal,
    Success,
    Error
}

public enum HintType
{
    Info,
    Finish,
    Critical
}

public delegate void HintHandler(
    string message,
    HintType type
);

public static class HintWrapper
{
    [Obsolete("请使用 Show 方法的 HintType 重载")]
    public static void Show(string message, HintTheme theme = HintTheme.Normal)
    {
        Show(message, theme switch
        {
            HintTheme.Normal => HintType.Info,
            HintTheme.Success => HintType.Finish,
            HintTheme.Error => HintType.Critical,
            _ => HintType.Info
        });
    }
    
    public static event HintHandler? OnShow;
    
    public static void Show(string message, HintType type = HintType.Info)
    {
        OnShow?.Invoke(message, type);
    }
}
