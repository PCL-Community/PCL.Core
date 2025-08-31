using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 全局配置服务。
/// </summary>
[LifecycleService(LifecycleState.Loading, Priority = 1919810)]
public sealed partial class ConfigService() : GeneralService("config", "配置服务")
{
    private static readonly Dictionary<string, object> _Items = [];

    public static bool TryGetConfigItem<TValue>(string key, [NotNullWhen(true)] out ConfigItem<TValue>? item)
    {
        var result = _Items.TryGetValue(key, out var value);
        item = result ? (ConfigItem<TValue>)value! : null;
        return result;
    }
    
    public static ConfigItem<TValue> GetConfigItem<TValue>(string key)
    {
        TryGetConfigItem<TValue>(key, out var item);
        return item ?? throw new KeyNotFoundException($"Config key not found: '{key}'");
    }
    
    public static IConfigProvider GetProvider(ConfigSource source) => source switch
    {
        // TODO
        _ => throw new ArgumentException("Invalid source")
    };
}
