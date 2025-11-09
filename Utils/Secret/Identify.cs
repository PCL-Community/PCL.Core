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

    private static SecureString _getRawId()
    {
        var code = new StringBuilder();
        try
        {
            code.Append(_GetWmiProperty("Win32_ComputerSystemProduct", "UUID"))
                .Append(_GetWmiProperty("Win32_BaseBoard", "Product"))
                .Append(_GetWmiProperty("Win32_BaseBoard", "SerialNumber"));
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Identify", "获取设备基础信息失败");
        }

        return SHA512Provider.Instance.ComputeHash(code.ToString()).ToSecureString();
    }

    private static string _GetWmiProperty(string className, string propertyName)
    {
        try
        {
            using var searcher =
                new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
            using var results = searcher.Get();
            foreach (var obj in results)
            {
                if (obj[propertyName] is not null)
                    return (obj[propertyName].ToString() ?? string.Empty).Trim();
            }
        }
        catch { /* Ignore */ }
        return string.Empty;
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