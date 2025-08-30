using System;
using System.Collections.Generic;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 用于监听配置项变更事件，而且本类中具有 ListenSetupChanged 属性的方法会被自动注册为配置项监听方法。<br/>
/// 监听回调接受<c>(object oldValue, object newValue, string? gamePath)</c>参数。<br/>
/// 在配置项被 Load / Set / Delete 的时候监听回调会被触发。<br/>
/// 类监听方法也可以直接接受具体类型的 oldValue 和 newValue 参数，传参时会做强制转型。<br/>
/// </summary>
/// <remarks>
/// 别在类监听方法中接受不同类型的 oldValue 和 newValue……
/// </remarks>
/// <example>
/// <code>
/// SetupListener.AddListener("UiCustomType", (oldValue, newValue, gamePath) => DoSomething());
/// </code>
/// <code>
/// [ListenSetupChanged("UiCustomType")]
/// public static void OnHomepageTypeChanged(int oldValue, int newValue, string? gamePath) { }
/// </code>
/// </example>
public static class SetupListener
{
    #region 事件注册与取消注册处理

    /// <summary>
    /// 加载本类的功能，调用后允许增删事件监听器
    /// </summary>
    public static void Load()
    {
        var dict = new Dictionary<string, List<ValueChangedHandler>>();
        // 调用生成代码直接注册本类中的方法
        SetupListenerRegisterer.RegisterClassListeners(dict);
        lock (dict)
            _handlersDict = dict;
        SetupService.SetupChanged += _AllChangedHandler;
    }

    /// <summary>
    /// 卸载本类的功能，调用后移除所有事件监听器且不可增加事件监听器
    /// </summary>
    public static void Unload()
    {
        SetupService.SetupChanged -= _AllChangedHandler;
        if (_handlersDict is { } dict)
            lock (dict)
                _handlersDict = null!;
    }

    /// <summary>
    /// 配置更改监听器，监听某个配置项的改变
    /// </summary>
    /// <param name="oldValue">旧值，非空</param>
    /// <param name="newValue">新值，非空</param>
    /// <param name="gamePath">游戏路径</param>
    public delegate void ValueChangedHandler(object oldValue, object newValue, string? gamePath);

    /// <summary>
    /// 添加一个配置更改监听器
    /// </summary>
    public static void AddListener(string keyName, ValueChangedHandler handler)
    {
        lock (_handlersDict)
        {
            if (!_handlersDict.TryGetValue(keyName, out var handlers))
                _handlersDict.Add(keyName, handlers = new());
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// 移除一个配置更改监听器
    /// </summary>
    public static void RemoveListener(string keyName, ValueChangedHandler handler)
    {
        lock (_handlersDict)
        {
            if (!_handlersDict.TryGetValue(keyName, out var handlers))
                return;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlersDict.Remove(keyName);
        }
    }

    private static Dictionary<string, List<ValueChangedHandler>> _handlersDict = null!;

    private static void _AllChangedHandler(SetupEntry entry, object? oldValue, object? newValue, string? gamePath)
    {
        ValueChangedHandler[] handlersCopy;
        lock (_handlersDict)
        {
            if (!_handlersDict.TryGetValue(entry.KeyName, out var handlers))
                return;
            handlersCopy = handlers.ToArray();
        }
        foreach (var handler in handlersCopy)
            handler.Invoke(oldValue ?? entry.DefaultValue, newValue ?? entry.DefaultValue, gamePath);
    }

    #endregion
}

#pragma warning disable CS9113

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
file sealed class ListenSetupChangedAttribute(string keyName) : Attribute;

#pragma warning restore CS9113