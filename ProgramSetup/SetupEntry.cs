using System;

namespace PCL.Core.ProgramSetup;

public sealed class SetupEntry<T>(string keyName, T defaultValue, SetupEntrySource source, bool isEncrypted = false)
{
    public readonly string KeyName = keyName;
    public readonly SetupEntrySource Source = source;
    public readonly bool IsEncrypted = isEncrypted;
    public readonly T DefaultValue = defaultValue;

    public SetupEntry(string keyName, SetupEntrySource source, bool isEncrypted = false)
        : this(keyName, GetDefaultDefaultValue(), source, isEncrypted) { }

    public T Value { get; set; }

    public static T GetDefaultDefaultValue()
    {
        var type = typeof(T);
        if (type == typeof(bool))
            return (T)(object)false;
        if (type == typeof(string))
            return (T)(object)string.Empty;
        if (type == typeof(int))
            return (T)(object)0;
        if (type == typeof(object))
            return (T)(object)null;
        throw new ArgumentException($"不支持为类型 {type} 提供默认值");
    }
}