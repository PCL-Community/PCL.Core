using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Net;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Scaffolding;

public static class SCFController
{
    private static bool _isHost;
    private static Socket? _socket;
    private static CancellationTokenSource? _cts;
    private static readonly ConcurrentDictionary<Guid, Socket> _Clients = new();

    public static int Launch(bool isHost)
    {
        try
        {
            _isHost = isHost;
            if (isHost)
            {
                _LaunchHost();
            }
            else
            {
                _LaunchClient();
            }
            return 0;
        }
        catch (Exception ex)
        {
            LogWrapper.Error("Link", $"scaffolding 启动失败: {ex.Message}");
            return 1;
        }
    }
    public static void Close()
    {
        try
        {
            _cts?.Cancel();
            
            // 当作为主机需要额外关闭所有客户端连接
            if (_isHost)
            {
                foreach (var client in _Clients.Values)
                {
                    try
                    {
                        client.SafeClose();
                    }
                    catch (Exception ex)
                    {
                        LogWrapper.Warn("Link", $"关闭客户端连接时发生错误: {ex.Message}");
                    }
                }
                _Clients.Clear();
            }
            
            _socket?.SafeClose();
            LogWrapper.Info("Link", "scaffolding 已关闭");
        }
        catch (Exception ex)
        {
            LogWrapper.Error("SCF", $"scaffolding 关闭时发生错误: {ex.Message}");
        }
    }
    #region 启动主机
    private static void _LaunchHost()
    {
        if (!_isHost)
        {
            throw new Exception("无法作为客户端启动主机");
        }
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192
        };
        var port = NetworkHelper.NewTcpPort();
        _socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
        _socket.Listen(100);
        LogWrapper.Info("Link", $"scaffolding 主机已启动，端口 {port}");
        
        // 开始接受客户端连接
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => _AcceptClientsAsync(_cts.Token), _cts.Token);
    }

    private static async Task _AcceptClientsAsync(CancellationToken token)
    {
        if (_socket == null)
        {
            return;
        }
        while (!token.IsCancellationRequested)
        {
            try
            {
                var clientSocket = await _socket.AcceptAsync(token);
                var clientId = Guid.NewGuid();
                _Clients[clientId] = clientSocket;
                
                LogWrapper.Info("Link", $"新客户端已连接，当前连接数: {_Clients.Count}");
                _ = Task.Run(() => _HandleClientAsync(clientId, token), token);
            }
            catch (OperationCanceledException)
            {
                // 取消操作，退出循环
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error("Link", $"接受客户端连接时发生错误: {ex.Message}");
            }
        }
    }
    
    private static async Task _HandleClientAsync(Guid clientId, CancellationToken token)
    {
        if (!_Clients.TryGetValue(clientId, out var clientSocket))
        {
            return;
        }
        var buffer = new byte[8192];
        try
        {
            while (!token.IsCancellationRequested)
            {
                var bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None, token);
                if (bytesRead == 0)
                {
                    // 客户端已断开连接
                    break;
                }
                
                var packet = ClientPacket.From(buffer);
                LogWrapper.Info("Link", $"收到客户端数据，类型: {packet.PacketType}, 长度: {packet.Body.Length}");
                
                // TODO 处理收到的数据并发送响应
            }
        }
        catch (OperationCanceledException)
        {
            // 取消操作，退出循环
        }
        catch (Exception ex)
        {
            LogWrapper.Error("Link", $"处理客户端数据时发生错误: {ex.Message}");
        }
        finally
        {
            clientSocket.SafeClose();
            _Clients.TryRemove(clientId, out _);
            LogWrapper.Info("Link", $"客户端已断开连接，当前连接数: {_Clients.Count}");
        }
    }
    #endregion
    #region 启动客户端
    private static void _LaunchClient()
    {
        if (_isHost)
        {
            throw new Exception("无法作为主机启动客户端");
        }
        if (TargetLobby == null)
        {
            throw new Exception("未选择目标大厅");
        }
        if (TargetLobby.Ip == null)
        {
            throw new Exception("目标大厅 IP 地址无效");
        }
        var hostEndpoint = new IPEndPoint(IPAddress.Parse(TargetLobby.Ip), 0); // TODO 需要解析主机HostName里面的端口信息
        
        _socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true,
            ReceiveBufferSize = 8192,
            SendBufferSize = 8192
        };
        _socket.Connect(hostEndpoint);
        LogWrapper.Info("Link", $"scaffolding 客户端已连接到主机");
        
        // 开始启动客户端循环
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => _ClientLoopAsync(_cts.Token), _cts.Token);
        _ = Task.Run(async () =>
        {
            var retries = 0;
            while (_cts.Token.IsCancellationRequested && retries <= 10)
            {
                // 获取主机支持的协议
                var protocols = "c:ping\0c:protocols\0c:server_port\0c:player_ping\0c:player_profile_list";
                var response = await _SendToHostAsync(_cts.Token, "c:protocols", protocols);
                if (response != null)
                {
                    if (response.StatusCode != 200)
                    {
                        LogWrapper.Warn("Link", $"主机响应异常，状态码: {response.StatusCode}");
                    }
                    else
                    {
                        var serverProtocols = Encoding.UTF8.GetString(response.Body).Split('\0', StringSplitOptions.RemoveEmptyEntries);
                        LogWrapper.Info("Link", $"收到主机响应, 状态: {response.StatusCode}, 支持的协议: {string.Join(", ", serverProtocols)}");
                        
                        // TODO 储存或处理主机支持的协议
                    }
                }
                else
                {
                    retries++;
                    LogWrapper.Warn("Link", $"未收到主机响应, 再尝试 { 10 - retries } 次");
                    await Task.Delay(1000, _cts.Token);
                    continue;
                }
            
                // 获取服务器端口
                response = await _SendToHostAsync(_cts.Token, "c:server_port", string.Empty);
                if (response != null)
                {
                    if (response.StatusCode != 200)
                    {
                        LogWrapper.Warn("Link", response.StatusCode == 32 ? "主机服务器未开启" : $"主机响应异常，状态码: {response.StatusCode}");
                    }
                    else
                    {
                        // TODO 大端序数字转换不会写, 到时候再说
                        break; // 成功获取端口后退出重试循环
                    }
                }
            }
        }, _cts.Token);
    }

    private static async Task _ClientLoopAsync(CancellationToken token)
    {
        var retries = 0;
        while (token.IsCancellationRequested && retries <= 10)
        {
            var body = new JsonObject
            {
                ["name"] = "", // TODO 玩家名称
                ["machine_id"] = "", // TODO 机器ID
                ["vendor"] = "PCL2-CE"
            };
            await _SendToHostAsync(token, "c:player_ping", body, false); // 发送心跳包

            var response =  await _SendToHostAsync(token, "c:player_profile_list", string.Empty); // 请求玩家列表
            if (response != null)
            {
                LogWrapper.Info("Link", $"收到主机响应, 状态: {response.StatusCode}, 长度: {response.Body.Length}");
                if (response.StatusCode != 200)
                {
                    LogWrapper.Warn("Link", $"主机响应异常，状态码: {response.StatusCode}");
                }
                else
                {
                    retries = 0;
                    // TODO 处理收到的数据
                }
            }
            else
            {
                retries++;
                LogWrapper.Warn("Link", $"未收到主机响应, 再尝试 { 10 - retries } 次");
                await Task.Delay(1000, token);
                continue;
            }
            await Task.Delay(5000, token);
        }
    }
    #region 发送数据到主机
    private static async Task<ServerPacket?> _SendToHostAsync(CancellationToken token, string type, JsonObject data, bool receiveAck = true)
    {
        return await _SendToHostAsync(token, type, data.ToJsonString(), receiveAck);
    }
    private static async Task<ServerPacket?> _SendToHostAsync(CancellationToken token, string type, string data, bool receiveAck = true)
    {
        if (_isHost)
        {
            LogWrapper.Warn("Link", "主机无法发送数据到自己");
            return null;
        }
        if (_socket is not { Connected: true })
        {
            LogWrapper.Warn("Link", "未连接到主机，无法发送数据");
            return null;
        }
        try
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var packet = new ClientPacket()
            {
                Body = dataBytes,
                PacketType = type
            };
            var bytes = packet.To();
            await _socket.SendAsync(bytes, token);
            LogWrapper.Info("Link", $"已发送数据到主机，类型: {type}, 长度: {bytes.Length}");

            if (!receiveAck)
            {
                return null;
            }
            var response = new byte[1024];
            await _socket.ReceiveAsync(response, token);
            var responsePacket = ServerPacket.From(response);
            return responsePacket;
        }
        catch (Exception ex)
        {
            LogWrapper.Error("Link", $"发送数据到主机时发生错误: {ex.Message}");
            return null;
        }
    }
    #endregion
    #endregion
}