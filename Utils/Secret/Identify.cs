using System;
using System.Management;
using System.Security;
using System.Text;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;

namespace PCL.Core.Utils.Secret;

public class Identify
{
    public static readonly Lazy<SecureString> RawId = new(_getRawId);
    public static readonly Lazy<SecureString> EncryptionKey = new(_getEncryptionKey);
    public static readonly Lazy<string> LauncherId = new(_getLauncherId);

    private const string DefaultRawId = "c34f48114e31f6bb99bdf720f0d98a3c74e325a90c3a8e638d3687a83d404b0c3687b6c0b7a752d718dd5e27a6be6ab68a7f8aa996de158052190bf3ffd17c61";

    private static SecureString _getRawId()
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
            LogWrapper.Error(ex, "Identify", $"WMI 查询失败，请检查组件是否正常");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            LogWrapper.Error(ex, "Identify", $"COM 异常，请确保WMI服务正在运行");
        }
        catch (UnauthorizedAccessException ex)
        {
            LogWrapper.Error(ex, "Identify", "访问被异常拒绝，请尝试以管理员权限运行");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Identify", $"意外的系统异常");
        }

        return (code is null ? DefaultRawId : SHA512Provider.Instance.ComputeHash(code)).ToSecureString();
    }

    private static SecureString _getEncryptionKey()
    {
        var prefix = "PCL-CE|"u8.ToArray();
        var ctx = RawId.Value.ToBytes();
        var suffix = "|EncryptionKey"u8.ToArray();

        var buffer = new byte[prefix.Length + ctx.Length + suffix.Length];
        var bufferSpan = buffer.AsSpan();
        prefix.CopyTo(bufferSpan[..prefix.Length]);
        ctx.CopyTo(bufferSpan.Slice(prefix.Length, ctx.Length));
        suffix.CopyTo(bufferSpan.Slice(prefix.Length + ctx.Length, suffix.Length));

        Array.Clear(ctx);
        var result = SHA256Provider.Instance.ComputeHash(bufferSpan).ToSecureString();
        bufferSpan.Clear();

        return result;
    }

    private static string _getLauncherId()
    {
        try
        {
            var prefix = "PCL-CE|"u8.ToArray();
            var ctx = RawId.Value.ToBytes();
            var suffix = "|LauncherId"u8.ToArray();

            var buffer = new byte[prefix.Length + ctx.Length + suffix.Length];
            var bufferSpan = buffer.AsSpan();
            prefix.CopyTo(bufferSpan[..prefix.Length]);
            ctx.CopyTo(bufferSpan.Slice(prefix.Length, ctx.Length));
            suffix.CopyTo(bufferSpan.Slice(prefix.Length + ctx.Length, suffix.Length));

            Array.Clear(ctx);
            var sample = SHA512Provider.Instance.ComputeHash(bufferSpan);
            bufferSpan.Clear();

            // 16 in length, 8 bytes, 64 bits, enough for us
            return sample.Substring(64, 16)
                .ToUpper()
                .Insert(4, "-")
                .Insert(9, "-")
                .Insert(14, "-");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Identify", "无法获取识别码");
            return "PCL2-CECE-GOOD-2025";
        }
    }
}