using System;

namespace PCL.Core.App.Configuration;

public enum ConfigStorage
{
    Shared,
    Local,
    Instance
}

public static class ConfigStorageExtension
{
    public static IConfigProvider GetProvider(this ConfigStorage storage) => storage switch
    {
        // TODO
        _ => throw new ArgumentException("Invalid storage type")
    };
}
