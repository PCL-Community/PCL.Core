using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using Ae.Dns.Client;
using Ae.Dns.Protocol;
using Ae.Dns.Protocol.Enums;
using Ae.Dns.Protocol.Records;
using PCL.Core.Logging;
using System.Runtime.Caching;

namespace PCL.Core.Net;

public class HostConnectionHandler
{
    public static HostConnectionHandler Instance { get; } = new();
    private const string ModuleName = "DoH";

    private static IDnsClient? _resolver;

    private HostConnectionHandler()
    {
        // 使用Ae.Dns创建DoH客户端，支持多个DoH服务器
        IDnsClient[] clients =
        [
            new DnsHttpClient(new HttpClient()
            {
                BaseAddress = new Uri("https://doh.pub/")
            }),
            new DnsHttpClient(new HttpClient()
            {
                BaseAddress = new Uri("https://doh.pysio.online/")
            }),
            new DnsHttpClient(new HttpClient()
            {
                BaseAddress = new Uri("https://cloudflare-dns.com/")
            })
        ];

        // 使用DnsRacerClient实现快速获胜策略
        _resolver = new DnsCachingClient(new DnsRacerClient(clients), new MemoryCache("DNS Query Cache"));
    }

    public async ValueTask<Stream> GetConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cts)
    {
        ArgumentNullException.ThrowIfNull(_resolver, "_resolver != null");
        // 获取主机名和端口
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        // 使用Ae.Dns解析IPv4和IPv6地址

        var queryA = _resolver.Query(DnsQueryFactory.CreateQuery(host), cts);
        var queryAAAA = _resolver.Query(DnsQueryFactory.CreateQuery(host, DnsQueryType.AAAA), cts);

        var resolveTasks = new List<Task<DnsMessage>>()
        {
            queryA,
            queryAAAA
        };

        var results = await Task.WhenAll(resolveTasks).ConfigureAwait(false);
        var addresses = (from result in results
            from answer in result.Answers
            where answer.Resource is DnsIpAddressResource
            select ((answer.Resource as DnsIpAddressResource)!).IPAddress).ToArray();

        if (addresses.Length == 0)
            throw new HttpRequestException($"No IP address for {host}");

        // 并行连接所有地址，返回第一个成功的连接
        var connectionTasks = addresses.Select(ip => _ConnectToAddressAsync(ip.ToString(), port, cts)).ToList();

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
            throw new HttpRequestException($"No address reachable: {host} -> {string.Join(", ", addresses.Select(x => x.ToString()))}");
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