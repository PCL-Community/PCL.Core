using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.UI;

public record MsgBoxButtonInfo(
    string Context,
    int Value = 0,
    Action? OnClick = null
);

public enum MsgBoxTheme
{
    Info,
    Warning,
    Error
}

public delegate void MsgBoxHandler(
    string message,
    string caption,
    ICollection<MsgBoxButtonInfo> buttons,
    MsgBoxTheme theme,
    bool block,
    ref int result
);

public static class MsgBoxWrapper
{
    public static event MsgBoxHandler? OnShow;

    public static int Show(
        string message,
        string caption,
        MsgBoxTheme theme,
        ICollection<MsgBoxButtonInfo> buttonCollection,
        bool block = true)
    {
        var result = 0;
        if (buttonCollection.Count == 0) buttonCollection = [new MsgBoxButtonInfo("确定")];
        OnShow?.Invoke(message, caption, buttonCollection, theme, block, ref result);
        return result;
    }

    public static int Show(
        string message,
        string caption = "提示",
        MsgBoxTheme theme = MsgBoxTheme.Info,
        bool block = true,
        params MsgBoxButtonInfo[] buttons)
    {
        return Show(message, caption, theme, buttonCollection: buttons, block);
    }

    public static int Show(
        string message,
        string caption = "提示",
        MsgBoxTheme theme = MsgBoxTheme.Info,
        bool block = true,
        params string[] buttons)
    {
        var index = 0;
        var list = buttons.Select(button => new MsgBoxButtonInfo(button, ++index)).ToList();
        return Show(message, caption, theme, buttonCollection: list, block);
    }
}
