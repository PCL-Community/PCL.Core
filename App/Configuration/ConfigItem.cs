using System;
using System.Collections.Generic;
using System.Linq;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 配置项。
/// </summary>
/// <typeparam name="TValue">值类型</typeparam>
public class ConfigItem<TValue>(
    string key,
    Func<TValue> defaultValue,
    ConfigSource source
) : IConfigScope
{
    private Func<TValue>? _defaultValueGetter = defaultValue;
    private TValue? _defaultValue;
    private bool _defaultValueHasSet = false;

    private TValue _GetDefaultValue()
    {
        if (_defaultValueHasSet) return _defaultValue!;
        _defaultValue = _defaultValueGetter!();
        _defaultValueHasSet = true;
        _defaultValueGetter = null;
        return _defaultValue;
    }

    /// <summary>
    /// 配置键。
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// 配置来源。
    /// </summary>
    public ConfigSource Source { get; set; } = source;

    /// <summary>
    /// 默认值。
    /// </summary>
    public TValue DefaultValue => _GetDefaultValue();

    public ConfigItem(string key, TValue defaultValue, ConfigSource source)
        : this(key, () => defaultValue, source) { }

    public IEnumerable<string> CheckScope(IReadOnlySet<string> keys) => keys.Contains(Key) ? [Key] : [];

    #region 值获取和修改

    private readonly IConfigProvider _provider = ConfigService.GetProvider(source);

    /// <summary>
    /// 获取配置值。
    /// </summary>
    /// <param name="argument">上下文参数</param>
    /// <returns>已设置的配置值或默认值</returns>
    public TValue GetValue(object? argument = null)
    {
        var exists = _provider.GetValue<TValue>(Key, out var value, argument);
        var e = TriggerEvent(ConfigEvent.Get, argument, value, true);
        if (e != null)
        {
            if (e.Cancelled) return DefaultValue;
            if (e.NewValueReplacement != null) return (TValue)e.NewValueReplacement;
        }
        return exists ? value! : DefaultValue;
    }

    /// <summary>
    /// 设置配置值。
    /// </summary>
    /// <param name="value">用于设置的值</param>
    /// <param name="argument">上下文参数</param>
    /// <returns>是否成功设置值，若成功则为 <c>true</c></returns>
    public bool SetValue(TValue value, object? argument = null)
    {
        var e = TriggerEvent(ConfigEvent.Set, argument, value);
        if (e != null)
        {
            if (e.Cancelled) return false;
            if (e.NewValueReplacement != null) value = (TValue)e.NewValueReplacement;
        }
        _provider.SetValue(Key, value, argument);
        return true;
    }

    /// <summary>
    /// 重置配置值，使其变为未设置状态。
    /// </summary>
    /// <param name="argument">上下文参数</param>
    /// <returns>是否成功重置值，若成功则为 <c>true</c></returns>
    public bool Reset(object? argument = null)
    {
        var e = TriggerEvent(ConfigEvent.Reset, argument, DefaultValue);
        if (e is { Cancelled: true }) return false;
        _provider.Delete(Key, argument);
        return true;
    }

    /// <summary>
    /// 检查配置值是否为默认值 (未设置状态)
    /// </summary>
    /// <param name="argument">上下文参数</param>
    public bool IsDefault(object? argument = null)
    {
        var result = !_provider.Exists(Key, argument);
        var e = TriggerEvent(ConfigEvent.CheckDefault, argument, result);
        if (e is { NewValueReplacement: not null }) result = (bool)e.NewValueReplacement;
        return result;
    }

    #endregion

    #region 事件监听

    private readonly HashSet<ConfigObserver> _observers = [];
    private readonly HashSet<ConfigObserver> _previewObservers = [];

    /// <summary>
    /// 传入事件观察器以观察事件。
    /// </summary>
    public void Observe(ConfigObserver observer)
    {
        if (observer.IsPreview) _previewObservers.Add(observer);
        else _observers.Add(observer);
    }

    /// <summary>
    /// 传入事件类型与处理委托以观察事件。
    /// </summary>
    public ConfigObserver Observe(ConfigEvent e, ConfigEventHandler handler, bool isPreview = false)
    {
        var observer = new ConfigObserver(e, handler, isPreview);
        Observe(observer);
        return observer;
    }

    /// <summary>
    /// 取消观察事件。
    /// </summary>
    public bool Unobserve(ConfigObserver observer)
        => observer.IsPreview ? _previewObservers.Remove(observer) : _observers.Remove(observer);

    // 获取值，若未设置则返回 null
    private object? _GetValueOrNull(object? argument, bool bypass)
    {
        if (bypass) return null;
        var exists = _provider.GetValue<TValue>(Key, out var value, argument);
        return exists ? value : null;
    }

    /// <summary>
    /// 触发配置项事件。
    /// </summary>
    /// <param name="trigger">触发事件</param>
    /// <param name="argument">上下文参数</param>
    /// <param name="newValue">用于向事件参数传递的新值</param>
    /// <param name="bypassOldValue">是否跳过旧值传递，若为 <c>true</c> 则向事件参数传递 <c>null</c>，默认为<c>false</c></param>
    /// <returns></returns>
    public ConfigEventArgs? TriggerEvent(ConfigEvent trigger, object? argument, object? newValue, bool bypassOldValue = false)
    {
        ConfigEventArgs? e = null;
        var replaceNewValue = false;
        foreach (var observer in (
            from observer in _previewObservers.Concat(_observers)
            let logic = (int)observer.Event & (int)trigger
            where logic > 0
            select observer
        )) {
            e ??= new ConfigEventArgs(Key, trigger, argument, _GetValueOrNull(argument, bypassOldValue), newValue);
            observer.Handler(e);
            // 对 preview 的特殊处理
            if (observer.IsPreview)
            {
                if (e.NewValueReplacement != null) replaceNewValue = true; // 记录替换操作
                if (e.Cancelled) return e;
            }
            // 防止非 preview 事件传递替换值
            else if (!replaceNewValue && e.NewValueReplacement != null) e.NewValueReplacement = null;
        }
        // 防止非 preview 事件传递取消状态
        if (e is { Cancelled: true }) e.Cancelled = false;
        return e;
    }

    #endregion
}
