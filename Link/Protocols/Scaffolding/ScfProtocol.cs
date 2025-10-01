using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Protocols.Scaffolding.Packet;
using PCL.Core.Logging;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using static PCL.Core.Link.Protocols.Scaffolding.ScfInfoProvider;
using PCL.Core.Net;

namespace PCL.Core.Link.Protocols.Scaffolding;

public class ScfProtocol(bool isServer) : LinkProtocol(isServer, "scaffolding")
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
    private string[] _serverSupportedProtocols = [];
    private readonly ConcurrentDictionary<Guid, ScfPlayerInfo> _playerList = new();

    protected override void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e)
    {
        // TODO: 实现数据接收处理逻辑
        _ = sender; // 避免未使用参数警告
        _ = e;      // 避免未使用参数警告
    }

    protected override async Task LaunchClient()
    {
        if (TargetLobby == null)
        {
            LogWrapper.Error("未设置目标大厅，无法启动客户端");
            return;
        }
        if (TargetLobby.Ip == null)
        {
            LogWrapper.Error("目标大厅 IP 地址未解析，无法启动客户端");
            return;
        }
        TcpHelper.Connect(TargetLobby.Ip, TargetLobby.ScfPort);
        _ = Task.Run(() => _ClientLoop(Ctx.Token), Ctx.Token);
        var packet = new ClientPacket
        {
            PacketType = "c:protocols",
            Body = Encoding.UTF8.GetBytes(string.Join("\0", _supportedProtocols))
        };
        var response = await TcpHelper.SendToServer(packet.To());
        if (response == null)
        {
            LogWrapper.Error("无法获取服务器响应");
            return;
        }

        _serverSupportedProtocols = Encoding.UTF8.GetString(ServerPacket.From(response).Body).Split('\0');
        
        packet = new ClientPacket
        {
            PacketType = "c:server_port",
            Body = []
        };
        response = await TcpHelper.SendToServer(packet.To());
        if (response == null)
        {
            LogWrapper.Error("无法获取服务器响应");
            return;
        }
        
        if (!ushort.TryParse(ServerPacket.From(response).Body, out var port))
        {
            LogWrapper.Error("服务器返回的端口号无效");
            return;
        }

        TargetLobby.Port = port;
        State = ProtocolState.Running;
        LogWrapper.Info($"{Identifier} 客户端已启动");
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
            var response = await TcpHelper.SendToServer(packet.To());

            await Task.Delay(5000, token);
        }
    }
    
    private byte[] _JsonObjectToBytes(JsonObject obj)
    {
        var jsonString = obj.ToJsonString();
        return Encoding.UTF8.GetBytes(jsonString);
    }
}