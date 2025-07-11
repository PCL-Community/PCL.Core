using System;
using System.Management;
using PCL.Core.Helper.Hash;

namespace PCL.Core.Helper;

public static class Identify
{
    private const string DefaultRawCode = "B09675A9351CBD1FD568056781FE3966DD936CC9B94E51AB5CF67EEB7E74C075";
    private static readonly Lazy<string?> _LazyCpuId = new(_GetCpuId);

    private static readonly Lazy<string> _LazyRawCode =
        new(() => CpuId is null ? DefaultRawCode : SHA256Provider.Instance.ComputeHash(CpuId).ToUpper());

    private static readonly Lazy<string> _LazyEncryptKey =
        new(() => SHA512Provider.Instance.ComputeHash(RawCode).Substring(4, 32).ToUpper());

    public static string GetGuid() => Guid.NewGuid().ToString();
    public static string? CpuId => _LazyCpuId.Value;
    public static string RawCode => _LazyRawCode.Value;
    public static string EncryptKey => _LazyEncryptKey.Value;

    private static string? _GetCpuId()
    {
        using var mos = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
        string? res = null;
        foreach (var item in mos.Get())
        {
            res = item["ProcessorId"]?.ToString();
            if (!string.IsNullOrEmpty(res)) break;
        }

        if (res is null)
            LogWrapper.Warn("Identify", "获取 cpu id 失败");
        return res;
    }

    public static string GetMachineId(string randomId)
    {
        return SHA512Provider.Instance.ComputeHash($"{randomId}|{CpuId}").ToUpper();
    }
}