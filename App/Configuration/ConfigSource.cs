﻿using System;

namespace PCL.Core.App.Configuration;

/// <summary>
/// 配置来源。
/// </summary>
public enum ConfigSource
{
    /// <summary>
    /// 全局共享配置。
    /// </summary>
    Shared,
    
    /// <summary>
    /// 本地配置。
    /// </summary>
    Local,
    
    /// <summary>
    /// 游戏实例特定配置。
    /// </summary>
    GameInstance
}

public static class ConfigSourceExtension
{
    public static IConfigProvider GetProvider(this ConfigSource source) => source switch
    {
        // TODO
        _ => throw new ArgumentException("Invalid source")
    };
}
