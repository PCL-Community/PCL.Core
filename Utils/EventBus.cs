using System;
using System.Collections.Generic;
using System.Security;
using System.Reflection;

namespace PCL.Core.Utils;

public class EventBus
{
    private static EventBus _instance = new();
    private Dictionary<string, List<Delegate>> _eventMap = new();

    public static ref EventBus GetInstance()
    {
        return ref _instance;
    }

    public EventBus On(string channel, Delegate handler)
    {
        if (!_eventMap.TryGetValue(channel, out List<Delegate>? value)) 
        {
            value = [];
            _eventMap.Add(channel, value);
        }

        value.Add(handler);
        return this;
    }

    [SecuritySafeCritical]
    public EventBus Emit(string channel, params object[] args) 
    {
        var impl = typeof(Delegate).GetMethod("DynamicInvoke", BindingFlags.Default);
        foreach (var handler in _eventMap[channel])
        {
            impl?.Invoke(handler, args);
        }
        return this;
    }

    public EventBus Off(string channel, Delegate handler)
    {
        _eventMap[channel].Remove(handler);
        return this;
    }
}

