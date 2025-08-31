using System;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 标记一个 partial 属性，以添加对应配置项并自动生成访问器。
/// </summary>
/// <param name="key">配置键</param>
/// <param name="source">配置来源</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigItemAttribute(string key, ConfigSource source = ConfigSource.Shared) : Attribute
{
    public string Key => key;
    public ConfigSource Source => source;
}

/// <summary>
/// 标记一个 partial 类为配置组，以自动实现 <see cref="IConfigScope"/> 并生成对应的作用域检查方法。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigGroupAttribute : Attribute;

/// <summary>
/// 标记一个与 <see cref="ConfigListener{T}"/> 签名兼容的 public static 方法，以监听配置项活动。
/// </summary>
/// <param name="scope">作用域</param>
/// <param name="listen">监听事件类型，使用按位或连接</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ConfigListenerAttribute(IConfigScope scope, ConfigEvent listen = ConfigEvent.Changed) : Attribute
{
    public IConfigScope Scope => scope;
    public ConfigEvent Listen => listen;
}
