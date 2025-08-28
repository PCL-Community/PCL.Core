using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Minecraft;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class ServerAddressResolver {
    /// <summary>
    /// 解析服务器地址并获取其可达的IP和端口。
    /// </summary>
    /// <param name="address">服务器地址，可以是IP、IP:端口或域名。</param>
    /// <returns>包含IP和端口的元组。</returns>
    /// <exception cref="ArgumentException">地址为空或无效。</exception>
    /// <exception cref="FormatException">端口格式无效或SRV记录格式无效。</exception>
    public static async Task<(string Ip, int Port)> GetReachableAddressAsync(string address) {
        // 输入验证
        if (string.IsNullOrWhiteSpace(address)) {
            throw new ArgumentException("服务器地址不能为空", nameof(address));
        }

        // 移除协议头（如果存在）
        address = address.Replace("http://", string.Empty).Replace("https://", string.Empty);

        // 情况1: IP:端口
        if (address.Contains(':')) {
            var parts = address.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port)) {
                throw new FormatException("无效的端口格式");
            }

            return (parts[0], port);
        }

        // 情况2: 纯IP (IPv4/IPv6)
        if (IPAddress.TryParse(address, out _)) {
            return (address, 25565);
        }

        // 情况3: 域名 (尝试SRV查询)
        try {
            LogWrapper.Info($"尝试SRV查询: _minecraft._tcp.{address}");
        
            // 关键修复：使用 .ToList() 将可枚举的集合转换为列表，确保只枚举一次。
            var srvRecords = (await _ResolveSrvRecordsAsync(address)).ToList();

            if (srvRecords.Count > 0) {
                var ret = _ParseSrvRecord(srvRecords.First());
                return ret;
            }
        } catch (SocketException ex) {
            LogWrapper.Warn(ex, "SRV查询失败 (网络错误)");
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "SRV查询异常");
        }

        // 默认: 直接使用域名+默认端口
        return (address, 25565);
    }

    private static async Task<IEnumerable<string>> _ResolveSrvRecordsAsync(string domain) {
        return await Task.Run(() => {
            try {
                return NDnsQuery.GetSRVRecords($"_minecraft._tcp.{domain}");
            } catch {
                return Enumerable.Empty<string>();
            }
        });
    }

    private static (string Host, int Port) _ParseSrvRecord(string record) {
        // 标准SRV格式: 优先级 权重 端口 主机
        // 按原VB.NET逻辑，先尝试按空格分割
        var parts = record.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && int.TryParse(parts[2], out var port)) {
            return (parts[3], port);
        }

        // 回退到原VB.NET处理逻辑，按冒号分割
        parts = record.Split(':');
        return parts.Length switch {
            2 when int.TryParse(parts[1], out var fallbackPort) => (parts[0], fallbackPort),
            1 => (parts[0], 25565),
            _ => throw new FormatException("无效的SRV记录格式")
        };
    }
}