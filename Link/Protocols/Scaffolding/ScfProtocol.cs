using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Protocols.Scaffolding.Packet;
using PCL.Core.Net;

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
    public List<object> PlayerList = [];

    protected override void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e)
    {
        // TODO: 实现数据接收处理逻辑
        _ = sender; // 避免未使用参数警告
        _ = e;      // 避免未使用参数警告
    }

    public override int Launch()
    {
        base.Launch();
        if (_isServer)
        {
            // TODO: 改为更详细的玩家信息
            TcpHelper.AcceptedClient += (s, e) =>
            {
                PlayerList.Add(e.ClientEndPoint);
            };
            TcpHelper.ClientDisconnected += (s, e) =>
            {
                PlayerList.Remove(e.ClientEndPoint);
            };
        }
        return 0;
    }

    protected override async Task ClientPart(CancellationToken token)
    {
        _ = Task.Run(() => _ClientLoop(token), token);
        var packet = new ClientPacket
        {
            PacketType = "c:protocols",
            Body = Encoding.UTF8.GetBytes(string.Join("\0", _supportedProtocols))
        };
        var response = await TcpHelper.SendToServer(packet.To()); // TODO: 处理响应数据
        
        packet = new ClientPacket
        {
            PacketType = "c:server_port",
            Body = []
        };
        response = await TcpHelper.SendToServer(packet.To()); // TODO: 处理响应数据
    }
    
    private async Task _ClientLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var packet = new ClientPacket
            {
                PacketType = "c:player_ping",
                Body = _JsonObjectToBytes(new JsonObject
                {
                    ["name"] = "", // TODO: 添加玩家信息
                    ["machine_id"] = "", // TODO: 添加玩家ID
                    ["vendor"] = "PCL2-CE"
                })
            };
            await TcpHelper.SendToServer(packet.To(), false);
            packet = new ClientPacket()
            {
                PacketType = "c:player_profile_list",
                Body = []
            };
            var response = await TcpHelper.SendToServer(packet.To()); // TODO: 处理响应数据

            await Task.Delay(5000, token);
        }
    }
    
    private byte[] _JsonObjectToBytes(JsonObject obj)
    {
        var jsonString = obj.ToJsonString();
        return Encoding.UTF8.GetBytes(jsonString);
    }
}