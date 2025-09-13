using System;
using System.Management;
using PCL.Core.Logging;
using PCL.Core.Utils.Hash;

namespace PCL.Core.Utils.Secret;

public class Identify
{
    public static readonly Lazy<string> RawId = new(_getRawId);
    public static readonly Lazy<string> EncryptionKey = new(_getEncryptionKey);
    public static readonly Lazy<string> LauncherId = new(_getLauncherId);

    private const string DefaultRawId = "c34f48114e31f6bb99bdf720f0d98a3c74e325a90c3a8e638d3687a83d404b0c3687b6c0b7a752d718dd5e27a6be6ab68a7f8aa996de158052190bf3ffd17c61";

    private static string _getRawId()
    {
        string? code = null;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            using var results = searcher.Get();
            foreach (var managementObject in results)
            {
                var uuid = managementObject?["UUID"];
                if (uuid is null) continue;
                code = uuid.ToString();
                break;
            }
        }
        catch (ManagementException ex)
        {
            LogWrapper.Error("Identify", $"WMI查询失败: {ex.Message}");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            LogWrapper.Error("Identify", $"COM异常: {ex.Message}. 请确保WMI服务正在运行");
        }
        catch (UnauthorizedAccessException)
        {
            LogWrapper.Error("Identify", "访问被拒绝，请以管理员权限运行");
        }
        catch (Exception ex)
        {
            LogWrapper.Error("Identify", $"意外的系统异常: {ex.Message}");
        }

        return code is null ? DefaultRawId : SHA512Provider.Instance.ComputeHash(code);
    }

    private static string _getEncryptionKey()
    {
        return SHA256Provider.Instance.ComputeHash($"PCL-CE|{RawId}|EncryptionKey");
    }

    private static string _getLauncherId()
    {
        try
        {
            var sample = SHA512Provider.Instance.ComputeHash($"PCL-CE|{RawId}|LauncherId");
            // 16 in length, 8 bytes, 64 bits, enough for us
            return sample.Substring(64, 16)
                .Insert(4, "-")
                .Insert(9, "-")
                .Insert(14, "-");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Identify", "无法获取短识别码");
            return "PCL2-CECE-GOOD-2025";
        }
    }
}