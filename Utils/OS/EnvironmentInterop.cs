using System;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Utils.OS;

public class EnvironmentInterop
{
    private const string ModuleEnvironment = "Environment";

    public static bool ReadVariable<TValue>(string key, ref TValue target, bool detailLog = true)
    {
        var envValue = Environment.GetEnvironmentVariable(key);
        if (envValue == null) return false;
        if (detailLog) LogWrapper.Trace(ModuleEnvironment, $"环境变量 {key} 值: {envValue}");
        var value = key.Convert<TValue>();
        if (value == null) return false;
        target = value;
        LogWrapper.Debug(ModuleEnvironment, $"成功读取环境变量 {key}");
        return true;
    }
}
