using System;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PCL.Core.Helper;

namespace PCL.Core.ProgramSetup.FileManager;

public sealed class SetupRegManager(string regPath) : ISetupFileManager
{
    private static readonly string[] NewLine = ["\r\n", "\r", "\n"];

    public string? Get(string key, string? mcPath)
    {
        try
        {
            using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true);
            return ProcessRegRawValue(regKey?.GetValue(key));
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "读取注册表项失败");
            return null;
        }
    }

    public string? Set(string key, string value, string? mcPath)
    {
        try
        {
            using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true)
                               ?? Registry.CurrentUser.CreateSubKey(regPath, true)
                               ?? throw new Exception("注册表键获取失败");
            var result = ProcessRegRawValue(regKey.GetValue(key));
            regKey.SetValue(key, value);
            return result;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "写入注册表项失败");
            return null;
        }
    }

    public string? Remove(string key, string? mcPath)
    {
        try
        {
            using var regKey = Registry.CurrentUser.OpenSubKey(regPath, true);
            var result = ProcessRegRawValue(regKey?.GetValue(key));
            try
            {
                regKey?.DeleteValue(key);
            }
            catch (ArgumentException)
            {
                /* 值不存在 */
            }
            return result;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "删除注册表项失败");
            return null;
        }
    }

    MultipleOperationHandle ISetupFileManager.BeginMultipleOperation(string? mcPath) => new(() => { });

    void IDisposable.Dispose() { }

    private static string? ProcessRegRawValue(object? rawValue)
    {
        return rawValue is null ? null : Regex.Replace(rawValue.ToString(), "\r\n|\r|\n", "");
    }
}