using System;
using PCL.Core.ProgramSetup.FileManager;
using PCL.Core.Service;

namespace PCL.Core.ProgramSetup;

public sealed class SetupEntry<T>(string keyName, T defaultValue, SetupEntrySource source, bool isEncrypted = false)
    where T : notnull
{
    private readonly Func<T, string> _serializer = Companion.GetSerializer<T>(isEncrypted);
    private readonly Func<string, T> _deserializer = Companion.GetDeserializer<T>(isEncrypted);
    private readonly ISetupFileManager _fileManager = Companion.GetFileManager(source);

    public SetupEntry(string keyName, SetupEntrySource source, bool isEncrypted = false)
        : this(keyName, Companion.GetDefaultDefaultValue<T>(), source, isEncrypted) { }

    /// <summary>
    /// 值改变时触发，参数分别是 mc 路径、旧值与新值，<br/>
    /// 若之前/现在配置文件中不存在该键将传入 <see langword="null"/>
    /// </summary>
    public event ValueChangedHandler? ValueChanged;

    public delegate void ValueChangedHandler(string? mcPath, Tuple<T>? oldValue, Tuple<T>? newValue);

    /// <summary>
    /// 获取该项的值，配置文件中不存在该键时返回 <see cref="defaultValue"/>
    /// </summary>
    public T Get(string? mcPath = null)
    {
        var rawValue = _fileManager.Get(keyName, mcPath);
        if (source == SetupEntrySource.SystemGlobal && rawValue is null)
        {
            var rawRegValue = Companion.RegManager.Remove(keyName, mcPath);
            if (rawRegValue is not null)
            {
                rawValue = !isEncrypted ? rawRegValue : Companion.Encrypt(Companion.DecryptOld(rawRegValue));
                _fileManager.Set(keyName, rawValue, mcPath);
            }
        }
        return rawValue is null ? defaultValue : _deserializer.Invoke(rawValue);
    }

    /// <summary>
    /// 设置该项的值，会触发 <see cref="ValueChanged"/> 事件
    /// </summary>
    public void Set(T value, string? mcPath = null)
    {
        var rawValue = _serializer.Invoke(value);
        var prevRawValue = _fileManager.Set(keyName, rawValue, mcPath);
        var prevValueTuple = DeserializeToTuple(prevRawValue);
        ValueChanged?.Invoke(mcPath, prevValueTuple, Tuple.Create(value));
    }

    /// <summary>
    /// 从配置文件中删除该键，会触发 <see cref="ValueChanged"/> 事件
    /// </summary>
    public void Reset(string? mcPath = null)
    {
        var prevRawValue = _fileManager.Remove(keyName, mcPath);
        if (source == SetupEntrySource.SystemGlobal)
        {
            var regValue = Companion.RegManager.Remove(keyName, mcPath);
            prevRawValue ??= regValue;
        }
        var prevValueTuple = DeserializeToTuple(prevRawValue);
        ValueChanged?.Invoke(mcPath, prevValueTuple, null);
    }

    /// <summary>
    /// 判断配置文件中是否含有该项的键
    /// </summary>
    /// <returns><see langword="true"/> - 如果配置文件中不含有该项的键</returns>
    public bool IsUnset(string? mcPath = null)
    {
        if (source == SetupEntrySource.SystemGlobal && Companion.RegManager.Get(keyName, mcPath) is not null)
        {
            return false;
        }
        return _fileManager.Get(keyName, mcPath) is null;
    }

    /// <summary>
    /// 手动触发一次 <see cref="ValueChanged"/> 事件
    /// </summary>
    public void RaiseChangedEvent(string? mcPath = null)
    {
        var rawValue = _fileManager.Get(keyName, mcPath);
        if (source == SetupEntrySource.SystemGlobal && rawValue is null)
        {
            var rawRegValue = Companion.RegManager.Remove(keyName, mcPath);
            if (rawRegValue is not null)
            {
                rawValue = !isEncrypted ? rawRegValue : Companion.Encrypt(Companion.DecryptOld(rawRegValue));
                _fileManager.Set(keyName, rawValue, mcPath);
            }
        }
        var valueTuple = DeserializeToTuple(rawValue);
        ValueChanged?.Invoke(mcPath, valueTuple, valueTuple);
    }

    private Tuple<T>? DeserializeToTuple(string? rawValue) =>
        rawValue is null ? null : Tuple.Create(_deserializer.Invoke(rawValue));
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

    public static Func<T, string> GetSerializer<T>(bool isEncrypted)
    {
        var type = typeof(T);
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if (isEncrypted)
        {
            if (type == typeof(int)) return v => Encrypt(((int)(object)v!).ToString());
            if (type == typeof(string)) return v => Encrypt((string)(object)v!);
            if (type == typeof(bool)) return v => Encrypt(((bool)(object)v!).ToString());
        }
        else
        {
            if (type == typeof(int)) return v => ((int)(object)v!).ToString();
            if (type == typeof(string)) return v => (string)(object)v!;
            if (type == typeof(bool)) return v => ((bool)(object)v!).ToString();
        }
        throw new ArgumentException($"不支持为类型 {type} 提供序列化器");
    }

    public static Func<string, T> GetDeserializer<T>(bool isEncrypted)
    {
        var type = typeof(T);
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        if (isEncrypted)
        {
            if (type == typeof(int)) return v => (T)(object)int.Parse(Decrypt(v));
            if (type == typeof(string)) return v => (T)(object)Decrypt(v);
            if (type == typeof(bool)) return v => (T)(object)bool.Parse(Decrypt(v));
        }
        else
        {
            if (type == typeof(int)) return v => (T)(object)int.Parse(v);
            if (type == typeof(string)) return v => (T)(object)v;
            if (type == typeof(bool)) return v => (T)(object)bool.Parse(v);
        }
        throw new ArgumentException($"不支持为类型 {type} 提供反序列化器");
    }

    public static ISetupFileManager GetFileManager(SetupEntrySource source)
    {
        return source switch
        {
            SetupEntrySource.PathLocal => SetupService.LocalSetupFile,
            SetupEntrySource.SystemGlobal => SetupService.GlobalSetupFile,
            SetupEntrySource.MinecraftInstance => SetupService.InstanceSetupFile,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, $"须为 {nameof(SetupEntrySource)} 枚举值")
        };
    }

    public static ISetupFileManager RegManager => SetupService.GlobalSetupReg;

    public static string Encrypt(string value) => throw new NotImplementedException();

    public static string Decrypt(string value) => throw new NotImplementedException();
    
    public static string DecryptOld(string value) => throw new NotImplementedException();
}