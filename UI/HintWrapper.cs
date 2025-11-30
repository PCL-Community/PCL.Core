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

[Obsolete("请使用 HintHandler 而不是 OldHintHandler")]
public delegate void OldHintHandler(
    string message,
    HintTheme theme
);

public delegate void HintHandler(
    string message,
    HintType type
);

public static class HintWrapper
{
    [Obsolete("请使用 OnShow 事件而不是 OldOnShow 事件")]
    public static event OldHintHandler? OldOnShow;

    [Obsolete("请使用 Show 方法的 HintType 重载")]
    public static void Show(string message, HintTheme theme = HintTheme.Normal)
    {
        OldOnShow?.Invoke(message, theme);
    }
    
    public static event HintHandler? OnShow;
    
    public static void Show(string message, HintType type = HintType.Info)
    {
        OnShow?.Invoke(message, type);
    }
}
