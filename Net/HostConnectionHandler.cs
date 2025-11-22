using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using DnsClientX;
using PCL.Core.Logging;
using PCL.Core.Utils.OS;

namespace PCL.Core.Net;

public class HostConnectionHandler
{
    public static HostConnectionHandler Instance { get; } = new();
    private const string ModuleName = "DoH";

    private static DnsMultiResolver? _resolver;

    private HostConnectionHandler()
    {
        var endpoints = EndpointParser.TryParseMany([
            "https://doh.pub/dns-query",
            "https://doh.pysio.online/dns-query",
            "https://cloudflare-dns.com/dns-query"
        ], out var errors);
        if (errors.Count != 0)
            LogWrapper.Error(ModuleName, $"Failed to resolve DoH endpoints: {string.Join(", ", errors)}");

        _resolver = new DnsMultiResolver(endpoints, new MultiResolverOptions
        {
            Strategy = MultiResolverStrategy.FastestWins,
            RespectEndpointTimeout = true
        });
    }

    public async ValueTask<Stream> GetConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cts)
    {
        ArgumentNullException.ThrowIfNull(_resolver, "_resolver != null");
        // 获取主机名和端口
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        // 并行解析 IPv4 和 IPv6 地址
        var resolveTasks = new List<Task<DnsResponse>>()
        {
            _resolver.QueryAsync(host, DnsRecordType.A, cts),
            _resolver.QueryAsync(host, DnsRecordType.AAAA, cts)
        };

        var results = await Task.WhenAll(resolveTasks).ConfigureAwait(false);
        var addresses = (from result in results
            from record in result.Answers
            where record.Type is DnsRecordType.A or DnsRecordType.AAAA
            select record.Data).ToArray();

        if (addresses.Length == 0)
            throw new HttpRequestException($"No IP address for {host}");

        // 并行连接所有地址，返回第一个成功的连接
        var connectionTasks = addresses.Select(ip => _ConnectToAddressAsync(ip, port, cts)).ToList();

        try
        {
            var completedTask = await Task.WhenAny(connectionTasks).ConfigureAwait(false);
            var stream = await completedTask.ConfigureAwait(false);

            // 取消其他连接任务
            foreach (var task in connectionTasks.Where(task => task != completedTask))
            {
                _ = task.ContinueWith(t => {
                    if (t.IsCompletedSuccessfully)
                    {
                        t.Result?.Dispose();
                    }
                }, cts);
            }

            LogWrapper.Debug(ModuleName, $"Success resolve DoH endpoint: {host} -> {stream.Socket.RemoteEndPoint}");
            return stream;
        }
        catch
        {
            throw new HttpRequestException($"No address reachable: {host} -> {string.Join(", ", addresses)}");
        }
    }

    private static async Task<NetworkStream> _ConnectToAddressAsync(string ip, int port, CancellationToken cts)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            using var ctsSocket = CancellationTokenSource.CreateLinkedTokenSource(cts);
            ctsSocket.CancelAfter(5000); // 5秒超时
            
            await socket.ConnectAsync(ip, port, ctsSocket.Token);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}