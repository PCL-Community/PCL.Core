using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.McLaunch.Services;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    
    public static void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }
    
    public static T GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? 
            (T)service : throw new InvalidOperationException($"Service {typeof(T)} not registered");
    }
}

