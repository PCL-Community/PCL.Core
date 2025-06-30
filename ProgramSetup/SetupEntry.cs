using System;

namespace PCL.Core.ProgramSetup;

public sealed class SetupEntry<T>(string keyName, T defaultValue, SetupEntrySource source, bool isEncrypted = false)
{
    public readonly string KeyName = keyName;
    public readonly SetupEntrySource Source = source;
    public readonly bool IsEncrypted = isEncrypted;
    public readonly T DefaultValue = defaultValue;
    private readonly Func<T, string> _serializer = Companion.GetSerializer<T>();
    private readonly Func<string, T> _deserializer = Companion.GetDeserializer<T>();
    private readonly ISetupFileManager _fileManager = Companion.GetFileManager(source);

    public SetupEntry(string keyName, SetupEntrySource source, bool isEncrypted = false)
        : this(keyName, Companion.GetDefaultDefaultValue<T>(), source, isEncrypted) { }

    public event Action<T?, T?>? ValueChanged;

    public T Get(string? mcPath = null)
    {
        throw new NotImplementedException();
    }

    public void Set(T value, string? mcPath = null, bool forceRaiseEvent = false)
    {
        throw new NotImplementedException();
    }

    public void Reset(string? mcPath = null)
    {
        throw new NotImplementedException();
    }

    public bool IsUnset(string? mcPath = null)
    {
        throw new NotImplementedException();
    }

    public void SyncValueFromDisk(string? mcPath = null) // Load
    {
        throw new NotImplementedException();
    }
}

file static class Companion
{
    public static T GetDefaultDefaultValue<T>()
    {
        var type = typeof(T);
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if (type == typeof(int)) return (T)(object)0;
        if (type == typeof(bool)) return (T)(object)false;
        if (type == typeof(string)) return (T)(object)string.Empty;
        throw new ArgumentException($"不支持为类型 {type} 提供默认值");
    }

    public static Func<T, string> GetSerializer<T>()
    {
        var type = typeof(T);
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if (type == typeof(int)) return v => ((int)(object)v!).ToString();
        if (type == typeof(string)) return v => (string)(object)v!;
        if (type == typeof(bool)) return v => ((bool)(object)v!).ToString();
        throw new ArgumentException($"不支持为类型 {type} 提供序列化器");
    }

    public static Func<string, T> GetDeserializer<T>()
    {
        var type = typeof(T);
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if (type == typeof(int)) return v => (T)(object)int.Parse(v);
        if (type == typeof(string)) return v => (T)(object)v!;
        if (type == typeof(bool)) return v => (T)(object)bool.Parse(v);
        throw new ArgumentException($"不支持为类型 {type} 提供反序列化器");
    }

    public static ISetupFileManager GetFileManager(SetupEntrySource source)
    {
        return source switch
        {
            SetupEntrySource.PathLocal => SetupManager.LocalSetupFile,
            SetupEntrySource.SystemGlobal => SetupManager.GlobalSetupFile,
            SetupEntrySource.MinecraftInstance => SetupManager.InstanceSetupFile,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, $"需为 {nameof(SetupEntrySource)} 枚举值")
        };
    }
}