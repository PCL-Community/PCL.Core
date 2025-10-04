using System;
using System.Buffers.Binary;
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

public class ScfProtocol : LinkProtocol
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

    private readonly ScfPlayerInfo _playerInfo;
    /// <summary>
    /// 连接 id -> 机器 id 映射
    /// </summary>
    private readonly ConcurrentDictionary<Guid, string> _connectionIds = new();
    
    public ScfProtocol(bool isServer, string hostname) : base(isServer, "scaffolding")
    {
        _playerInfo = new ScfPlayerInfo
        {
            Name = hostname,
            MachineId = hostname, // TODO: 获取machineId(交给鸽秋)
            Vendor = "PCL-CE",
            IsHost = isServer
        };
        if (isServer)
        {
            PlayerList.TryAdd(_playerInfo.MachineId, _playerInfo);
        }
    }

    protected override void AcceptedClient(object? sender, TcpHelper.HandleClientEventArgs e)
    {
        _connectionIds.TryAdd(e.ConnectionId, String.Empty); // 先添加一个空的机器 id, 后续收到玩家信息后更新
        LogWrapper.Info($"{Identifier} 有新客户端连接, 连接 ID: {e.ConnectionId}");
    }

    protected override void ClientDisconnected(object? sender, TcpHelper.HandleClientEventArgs e)
    {
        if (_connectionIds.TryGetValue(e.ConnectionId, out var machineId))
        {
            _connectionIds.TryRemove(e.ConnectionId, out _);
            PlayerList.TryRemove(machineId, out _);
            LogWrapper.Info($"{Identifier} 客户端断开连接, 连接 ID: {e.ConnectionId}, 机器 ID: {machineId}");
        }
        else
        {
            LogWrapper.Info($"{Identifier} 客户端断开连接, 连接 ID: {e.ConnectionId}");
        }
    }

    protected override void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e)
    {
        var packet = ClientPacket.From(e.Data);
        LogWrapper.Info($"{Identifier} 收到数据包, 类型:{packet.PacketType}");
        ServerPacket? response = null;
        switch (packet.PacketType)
        {
            case "c:ping":
                response = new ServerPacket
                {
                    StatusCode = 0,
                    Body = packet.Body
                };
                break;
            case "c:protocols":
                response = new ServerPacket
                {
                    StatusCode = 0,
                    Body = Encoding.UTF8.GetBytes(string.Join("\0", _supportedProtocols))
                };
                break;
            case "c:server_port":
                if (TargetLobby == null)
                {
                    LogWrapper.Error("未设置目标大厅，无法获取服务器端口");
                    return;
                }
                var portBytes = new byte[2];
                BinaryPrimitives.WriteUInt16BigEndian(portBytes, (ushort)TargetLobby.Port);
                response = new ServerPacket
                {
                    StatusCode = 0,
                    Body = portBytes
                };
                break;
            case "c:player_ping":
                var clientPlayerInfo = JsonNode.Parse(Encoding.UTF8.GetString(packet.Body)) as JsonObject;
                if (clientPlayerInfo == null)
                {
                    LogWrapper.Error("玩家信息解析错误");
                    return;
                }
                
                if (clientPlayerInfo.TryGetPropertyValue("machine_id", out var jsonMachineId))
                {
                    var machineId = jsonMachineId!.GetValue<string>();
                    _connectionIds[e.ConnectionId] = machineId; // 更新连接ID到机器ID的映射
                    
                    if (!PlayerList.TryAdd(machineId, new ScfPlayerInfo
                        {
                            Name = clientPlayerInfo["name"]?.GetValue<string>() ?? string.Empty,
                            MachineId = machineId,
                            Vendor = "PCL-CE",
                            IsHost = false
                        }))
                    {
                        PlayerList[machineId].Name = clientPlayerInfo["name"]?.GetValue<string>() ?? string.Empty;
                    }
                }
                break;
            default:
                return;
        }
        e.Response = response?.To();
    }

    protected override async Task LaunchClientAsync()
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
        _ = Task.Run(() => _ClientLoopAsync(Ctx.Token), Ctx.Token);
        var packet = new ClientPacket
        {
            PacketType = "c:protocols",
            Body = Encoding.UTF8.GetBytes(string.Join("\0", _supportedProtocols))
        };
        await TcpHelper.SendToServerAsync(packet.To(), false);
        
        packet = new ClientPacket
        {
            PacketType = "c:server_port",
            Body = []
        };
        var response = await TcpHelper.SendToServerAsync(packet.To());
        if (response == null)
        {
            LogWrapper.Error("无法获取服务器响应");
            return;
        }
        
        if (response.Length != 2)
        {
            LogWrapper.Error("服务器响应长度错误");
            return;
        }
        
        var port = BinaryPrimitives.ReadUInt16BigEndian(response);

        TargetLobby.Port = port;
        State = ProtocolState.Running;
        LogWrapper.Info($"{Identifier} 客户端已启动");
    }
    
    private async Task _ClientLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var packet = new ClientPacket
            {
                PacketType = "c:player_ping",
                Body = _JsonObjectToBytes(_playerInfo.ToJsonObject())
            };
            await TcpHelper.SendToServerAsync(packet.To(), false);
            LogWrapper.Info($"{Identifier} 已发送玩家信息");
            
            packet = new ClientPacket
            {
                PacketType = "c:player_profile_list",
                Body = []
            };
            var response = await TcpHelper.SendToServerAsync(packet.To());
            if (response == null)
            {
                LogWrapper.Error("无法获取服务器响应");
                continue;
            }

            if (JsonNode.Parse(Encoding.UTF8.GetString(ServerPacket.From(response).Body)) is JsonArray playerList)
            {
                PlayerList.Clear();
                foreach (var item in playerList)
                {
                    var playerInfo = ScfPlayerInfo.FromJsonObject(item as JsonObject);
                    PlayerList.TryAdd(playerInfo.MachineId, playerInfo);
                }
            }
            LogWrapper.Info($"{Identifier} 玩家列表已更新");
            await Task.Delay(5000, token);
        }
    }
    
    private byte[] _JsonObjectToBytes(JsonObject obj)
    {
        var jsonString = obj.ToJsonString();
        return Encoding.UTF8.GetBytes(jsonString);
    }

    public override int Close()
    {
        PlayerList.Clear();
        return base.Close();
    }
}