using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PCL.Core.Net;

public partial class NDnsQuery {
    [LibraryImport("dnsapi", EntryPoint = "DnsQuery_W", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int _DnsQuery(string pszName, QueryTypes wType, QueryOptions options, int aipServers, ref IntPtr ppQueryResults, int pReserved);

    [LibraryImport("dnsapi")]
    private static partial void _DnsRecordListFree(IntPtr pRecordList, int freeType);

    public static List<string> GetSRVRecords(string needle) {
        var ptr1 = IntPtr.Zero;

        if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
            throw new NotSupportedException("此方法仅支持 Windows NT 及更高版本操作系统。");
        }

        var res = new List<string>();
        try {
            var num1 = _DnsQuery(needle, QueryTypes.DNS_TYPE_SRV, QueryOptions.DNS_QUERY_STANDARD, 0, ref ptr1, 0);
            if (num1 != 0) {
                // 9003 is DNS_ERROR_RCODE_NAME_ERROR, meaning the name doesn't exist
                return num1 == 9003 ? [] : throw new Win32Exception(num1);
            }

            var ptr2 = ptr1;
            while (!ptr2.Equals(IntPtr.Zero)) {
                var recSRV = Marshal.PtrToStructure<SRVRecord>(ptr2);
                
                if (recSRV.wType == (ushort)QueryTypes.DNS_TYPE_SRV) {
                    var targetIp = Marshal.PtrToStringUni(recSRV.pNameTarget);
                    var targetPort = recSRV.wPort.ToString();
                    res.Add($"{targetIp}:{targetPort}");
                }

                ptr2 = recSRV.pNext;
            }
        } finally {
            _DnsRecordListFree(ptr1, 0);
        }

        return res;
    }

    private enum QueryOptions : uint {
        DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 1,
        DNS_QUERY_BYPASS_CACHE = 8,
        DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
        DNS_QUERY_NO_HOSTS_FILE = 0x40,
        DNS_QUERY_NO_LOCAL_NAME = 0x20,
        DNS_QUERY_NO_NETBT = 0x80,
        DNS_QUERY_NO_RECURSION = 4,
        DNS_QUERY_NO_WIRE_QUERY = 0x10,
        DNS_QUERY_RESERVED = 0xFF000000,
        DNS_QUERY_RETURN_MESSAGE = 0x200,
        DNS_QUERY_STANDARD = 0,
        DNS_QUERY_TREAT_AS_FQDN = 0x1000,
        DNS_QUERY_USE_TCP_ONLY = 2,
        DNS_QUERY_WIRE_ONLY = 0x100
    }

    private enum QueryTypes {
        DNS_TYPE_A = 0x1,
        DNS_TYPE_MX = 0xF,
        DNS_TYPE_SRV = 0x21
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SRVRecord {
        public IntPtr pNext;
        public IntPtr pName;
        public ushort wType;
        public ushort wDataLength;
        public int flags;
        public int dwTtl;
        public int dwReserved;
        public IntPtr pNameTarget;
        public ushort wPriority;
        public ushort wWeight;
        public ushort wPort;
        public ushort Pad;
    }
}