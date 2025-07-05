using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Helper.Hash;

namespace PCL.Core.Helper;

public static class Identify
{
    public static string GetGuid() => Guid.NewGuid().ToString();

    public static string GetCpuId()
    {
        using var mos = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
        string? res = null;
        foreach (var item in mos.Get())
        {
            res = item["ProcessorId"]?.ToString();
            if (!string.IsNullOrEmpty(res)) break;
        }

        return res ?? "UNKNOWN CPU ID";
    }

    public static string GetMachineId(string randomId)
    {
        return SHA512Provider.Instance.ComputeHash($"{randomId}|{GetCpuId()}").ToUpper();
    }
}