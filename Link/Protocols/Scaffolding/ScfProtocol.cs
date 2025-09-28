using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Net;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Link.Protocols.Scaffolding;

public class ScfProtocol(bool isServer) : LinkProtocol(isServer)
{
    protected override string Identifier => "scaffolding";
    private readonly string[] _supportedProtocols = 
    [
        "c:ping",
        "c:protocols",
        "c:server_port",
        "c:player_ping",
        "c:player_profile_list"
    ];

    protected override void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e)
    {
        // TODO: 实现数据接收处理逻辑
        _ = sender; // 避免未使用参数警告
        _ = e;      // 避免未使用参数警告
    }
    
    protected override async Task ClientPart(CancellationToken token)
    {
        _ = Task.Run(() => ClientLoop(token), token);
    }
    
    private async Task ClientLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // TODO 等待ClientPacket类写好
        }
    }
}