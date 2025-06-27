using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

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
        return HashHelper.ComputeSHA512($"{randomId}|{GetCpuId()}");
    }
}