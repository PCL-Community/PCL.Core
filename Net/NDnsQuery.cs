using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PCL.Core.Net;

public static partial class NDnsQuery
{
    private static readonly object _QueryLock = new();

    /// <summary>
    /// 调用系统函数查询 SRV 记录。<br/>
    /// 注意：由于地址空间差异，该实现仅支持 64 位系统。
    /// </summary>
    /// <param name="needle">要查询的域名</param>
    /// <returns>查询结果，可能有多条，若查询不到则为空列表</returns>
    /// <exception cref="NotSupportedException">在非 64 位系统上调用</exception>
    /// <exception cref="Win32Exception">内部方法出错</exception>
    public static List<string> GetSrvRecords(string needle)
    {
        lock (_QueryLock)
        {
            nint pResults = 0;

            // 防止爆炸
            if (!Environment.Is64BitOperatingSystem) throw new NotSupportedException("仅支持 64 位系统");

            var res = new List<string>();
            try
            {
                var status = _DnsQuery(
                    needle,
                    (ushort)QueryTypes.DNS_TYPE_SRV,
                    QueryOptions.DNS_QUERY_STANDARD,
                    0, // pServerList/pExtra
                    out pResults, // PDNS_RECORD*
                    0 // pReserved
                );

                if (status != 0)
                {
                    // 9003 = DNS_ERROR_RCODE_NAME_ERROR（域名不存在）
                    return status == 9003 ? res : throw new Win32Exception(status);
                }

                var pCur = pResults;
                while (pCur != 0)
                {
                    var rec = Marshal.PtrToStructure<DNS_RECORD_SRV>(pCur);

                    if (rec.wType == (ushort)QueryTypes.DNS_TYPE_SRV)
                    {
                        if (rec.pNameTarget != 0)
                        {
                            var target = Marshal.PtrToStringUni(rec.pNameTarget);
                            if (!string.IsNullOrEmpty(target))
                            {
                                // SRV 目标常见以 '.' 结尾，去掉以便后续使用
                                target = _TrimTrailingDot(target);
                                res.Add($"{target}:{rec.wPort}");
                            }
                        }
                    }

                    pCur = rec.pNext;
                }
            }
            finally
            {
                if (pResults != 0) _DnsRecordListFree(pResults, 0);
            }

            return res;
        }
    }

    // ReSharper disable InconsistentNaming, UnusedMember.Local

    private static string _TrimTrailingDot(string s) => s.Length > 0 && s[^1] == '.' ? s[..^1] : s;

    [LibraryImport("dnsapi", EntryPoint = "DnsQuery_W", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int _DnsQuery(string pszName, ushort wType, QueryOptions options, nint pServerList, out nint ppQueryResults, nint pReserved);

    [LibraryImport("dnsapi", EntryPoint = "DnsRecordListFree")]
    private static partial void _DnsRecordListFree(nint pRecordList, int freeType);

    [Flags]
    private enum QueryOptions : uint
    {
        DNS_QUERY_STANDARD = 0,
        DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 1,
        DNS_QUERY_USE_TCP_ONLY = 2,
        DNS_QUERY_NO_RECURSION = 4,
        DNS_QUERY_BYPASS_CACHE = 8,
        DNS_QUERY_NO_WIRE_QUERY = 0x10,
        DNS_QUERY_NO_LOCAL_NAME = 0x20,
        DNS_QUERY_NO_HOSTS_FILE = 0x40,
        DNS_QUERY_NO_NETBT = 0x80,
        DNS_QUERY_TREAT_AS_FQDN = 0x1000,
        DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
        DNS_QUERY_RETURN_MESSAGE = 0x200,
        DNS_QUERY_WIRE_ONLY = 0x100,
        DNS_QUERY_RESERVED = 0xFF000000
    }

    private enum QueryTypes : ushort
    {
        DNS_TYPE_A = 0x0001,
        DNS_TYPE_MX = 0x000F,
        DNS_TYPE_SRV = 0x0021
    }

    // 与 DNS_RECORDW 针对 SRV 的内存布局匹配
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    private struct DNS_RECORD_SRV
    {
        public nint pNext;
        public nint pName;
        public ushort wType;
        public ushort wDataLength;
        public uint flags;      // union Flags
        public uint dwTtl;
        public uint dwReserved;

        // SRV union payload
        public nint pNameTarget;
        public ushort wPriority;
        public ushort wWeight;
        public ushort wPort;
        public ushort Pad;
    }
}
