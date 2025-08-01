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
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            using ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementBaseObject item in collection)
            {
                try
                {
                    string? cpuId = item["ProcessorId"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(cpuId))
                    {
                        return cpuId;
                    }
                }
                catch (ManagementException ex)
                {
                    LogWrapper.Warn("Identify", $"WMI属性读取失败: {ex.Message}");
                }
                finally
                {
                    item.Dispose();
                }
            }

            LogWrapper.Warn("Identify", "未找到有效的CPU ID");
            return null;
        }
        catch (ManagementException ex)
        {
            LogWrapper.Error("Identify", $"WMI查询失败: {ex.Message}");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            LogWrapper.Error("Identify", $"COM异常: {ex.Message}. 请确保WMI服务正在运行");
        }
        catch (System.UnauthorizedAccessException)
        {
            LogWrapper.Error("Identify", "访问被拒绝，请以管理员权限运行");
        }
        catch (Exception ex)
        {
            LogWrapper.Error("Identify", $"意外的系统异常: {ex.Message}");
        }

        return null;
    }

    public static string GetMachineId(string randomId)
    {
        return SHA512Provider.Instance.ComputeHash($"{randomId}|{CpuId}").ToUpper();
    }
}