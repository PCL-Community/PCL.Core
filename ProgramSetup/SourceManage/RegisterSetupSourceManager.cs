using System;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PCL.Core.Logging;

namespace PCL.Core.ProgramSetup.SourceManage;

public sealed class RegisterSetupSourceManager(string regPath) : ISetupSourceManager
{
    #region ISetupSourceManager

    public string? Get(string key, string? gamePath)
    {
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        try
        {
            lock (_regProcLock)
            {
                using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true);
                return _ProcessRegRawValue(regKey?.GetValue(key));
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Setup", "读取注册表项失败");
            throw;
        }
    }

    [Obsolete]
    public string? Set(string key, string value, string? gamePath)
    {
        LogWrapper.Warn("Setup", $"不应该调用 {nameof(RegisterSetupSourceManager)}::{nameof(Set)}，因为注册表配置源已不再使用");
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        try
        {
            lock (_regProcLock)
            {
                using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true) 
                                   ?? Registry.CurrentUser.CreateSubKey(regPath, true);
                string? result = _ProcessRegRawValue(regKey.GetValue(key));
                regKey.SetValue(key, value);
                return result;
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Setup", "写入注册表项失败");
            throw;
        }
    }

    public string? Remove(string key, string? gamePath)
    {
        if (gamePath is not null)
            throw new ArgumentException("获取非游戏实例配置时错误地提供了游戏路径", nameof(gamePath));
        try
        {
            lock (_regProcLock)
            {
                using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true);
                if (regKey is null)
                    return null;
                string? result = _ProcessRegRawValue(regKey.GetValue(key));
                try { regKey.DeleteValue(key); } catch (ArgumentException) { /* 键不存在 */ }
                return result;
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Setup", "删除注册表项失败");
            throw;
        }
    }

    #endregion

    private readonly object _regProcLock = new();

    private static string? _ProcessRegRawValue(object? rawValue)
    {
        return rawValue is null ? null : Regex.Replace(rawValue.ToString(), "\r\n|\r|\n", "");
    }
}