using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 全局配置服务。
/// </summary>
[LifecycleService(LifecycleState.Loading, Priority = 1919810)]
public sealed partial class ConfigService() : GeneralService("config", "配置")
{
    private static readonly Dictionary<string, IEventScope> _Items = [];

    private static readonly HashSet<string> _KeySet = [];

    /// <summary>
    /// 配置键的集合。
    /// </summary>
    public static IReadOnlySet<string> KeySet => _KeySet;

    /// <summary>
    /// 全局配置文件的版本号。
    /// </summary>
    [ConfigItem<int>("FileVersion", 1)] public static partial int SharedVersion { get; set; }

    /// <summary>
    /// 本地配置文件的版本号。
    /// </summary>
    [ConfigItem<int>("FileVersion", 1, ConfigSource.Local)] public static partial int LocalVersion { get; set; }

    #region Getters & Setters

    /// <summary>
    /// 尝试获取配置项的可观察对象。
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="item">返回可观察对象</param>
    /// <returns>若配置键存在，则为 <c>true</c>，否则为 <c>false</c></returns>
    public static bool TryGetObservableItem(string key, [NotNullWhen(true)] out IEventScope? item)
        => _Items.TryGetValue(key, out item);

    /// <summary>
    /// 尝试获取配置项。
    /// </summary>
    /// <param name="key">配置键</param>
    /// <param name="item">返回配置项，若类型不匹配则为 <c>null</c></param>
    /// <typeparam name="TValue">配置项的值类型</typeparam>
    /// <returns>若配置键存在，则为 <c>true</c>，否则为 <c>false</c></returns>
    /// <exception cref="InvalidOperationException">配置项尚未初始化完成</exception>
    public static bool TryGetConfigItem<TValue>(string key, out ConfigItem<TValue>? item)
    {
        if (!_isConfigItemsInitialized) throw new InvalidOperationException("Not initialized");
        var result = TryGetObservableItem(key, out var value);
        item = result ? (value as ConfigItem<TValue>) : null;
        return result;
    }

    /// <summary>
    /// 获取配置项。
    /// </summary>
    /// <param name="key">配置键</param>
    /// <typeparam name="TValue">配置项的值类型</typeparam>
    /// <returns>配置项实例</returns>
    /// <exception cref="InvalidOperationException">配置项尚未初始化完成</exception>
    /// <exception cref="KeyNotFoundException">配置键不存在</exception>
    /// <exception cref="InvalidCastException">值类型参数与实际类型不匹配</exception>
    public static ConfigItem<TValue> GetConfigItem<TValue>(string key)
    {
        var result = TryGetConfigItem<TValue>(key, out var item);
        if (!result) throw new KeyNotFoundException($"Config key not found: '{key}'");
        return item ?? throw new InvalidCastException($"Type of '{key}' is incompatible with {typeof(TValue).FullName}");
    }

    /// <summary>
    /// 获取配置提供方。
    /// </summary>
    /// <param name="source">来源定义</param>
    /// <returns>提供方实例</returns>
    /// <exception cref="InvalidOperationException">配置提供方尚未初始化完成</exception>
    /// <exception cref="ArgumentException">来源定义无效</exception>
    public static IConfigProvider GetProvider(ConfigSource source)
    {
        if (!_isProvidersInitialized) throw new InvalidOperationException("Not initialized");
        return source switch
        {
            // TODO
            _ => throw new ArgumentException($"Invalid source: {source}")
        };
    }

    /// <summary>
    /// 向指定作用域批量注册事件观察器。
    /// </summary>
    /// <param name="scope"><see cref="IConfigScope"/> 实例</param>
    /// <param name="observer">观察器实例</param>
    public static void RegisterObserver(IConfigScope scope, ConfigObserver observer)
    {
        var itemKeys = scope.CheckScope(KeySet);
        foreach (var key in itemKeys)
        {
            var item = _Items[key];
            item.Observe(observer);
        }
    }

    #endregion

    #region Lifecycle & Initialization

    /// <summary>
    /// 配置服务是否已加载完成。未加载完成时，调用与配置项相关的方法可能会抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;

    private static bool _isProvidersInitialized = false;
    private static bool _isConfigItemsInitialized = false;

    public override void Start()
    {
        if (IsInitialized) return;
#if TRACE
        var timer = new Stopwatch();
        timer.Start();
#endif
        ServiceContext.Info("Config initialization started");
        ServiceContext.Trace("Initializing providers...");
        _InitializeProviders();
        _isProvidersInitialized = true;
        ServiceContext.Trace("Initializing config items...");
        _InitializeConfigItems();
        ServiceContext.Debug($"Finished initialize {_Items.Count} item(s)");
        _isConfigItemsInitialized = true;
        ServiceContext.Trace("Initializing observers...");
        _InitializeObservers();
        ServiceContext.Info("Invoking init events...");
        foreach (var (_, item) in _Items)
        {
            item.TriggerEvent(ConfigEvent.Init, null, null, true, true);
        }
        IsInitialized = true;
#if TRACE
        timer.Stop();
        ServiceContext.Info($"Config initialization finished in {timer.ElapsedMilliseconds} ms");
#endif
    }

    private static void _InitializeProviders()
    {
    }

    #endregion
}
