using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public class TcpHelper : IDisposable
{
    /// <summary>
    /// 是否为服务器, 用于判断使用哪种方法
    /// </summary>
    private readonly bool _isServer;
    private readonly Socket _socket;
    private readonly CancellationTokenSource _ctx;
    
    public class ReceivedDateEventArgs(byte[] data, Socket clientSocket) : EventArgs
    {
        public byte[] Data => data;
        /// <summary>
        /// 客户端Socket, 用于事件中可能的回复
        /// </summary>
        public Socket ClientSocket => clientSocket;
    }
    public event EventHandler<ReceivedDateEventArgs>? ReceivedData;
    public event EventHandler? ClientDisconnected;

    public TcpHelper(bool isServer, string serverIp = "", int serverPort = 0)
    {
        _ctx = new CancellationTokenSource();
        _isServer = isServer;
        if (_isServer)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, NetworkHelper.NewTcpPort()));
            _ = Task.Run(() => _AcceptConnections(_ctx.Token), _ctx.Token);
        }
        else
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (serverIp == "")
            {
                throw new Exception("客户端必须指定服务器IP");
            }
            if (serverPort == 0)
            {
                throw new Exception("客户端必须指定服务器端口");
            }
            if (!IPAddress.TryParse(serverIp, out var serverAddress))
            {
                throw new Exception("服务器IP格式错误");
            }
            _socket.Connect(new IPEndPoint(serverAddress, serverPort));
        }
    }
    
    private async Task _AcceptConnections(CancellationToken token)
    {
        try
        {
            if (!_isServer)
            {
                throw new Exception("客户端不能调用AcceptConnections方法");
            }
            _socket.Listen(10);
            while (!token.IsCancellationRequested)
            {
                var clientSocket = await _socket.AcceptAsync(token);
                _ = Task.Run(() => _HandleClient(clientSocket, token), token);
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "接受客户端连接时发生错误");
        }
    }

    public async Task _HandleClient(Socket clientSocket, CancellationToken token)
    {
        var buffer = new byte[1024];
        try
        {
            while (!token.IsCancellationRequested)
            {
                var received = await clientSocket.ReceiveAsync(buffer, token);
                if (received == 0)
                {
                    break; // 客户端断开连接
                }
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                ReceivedData?.Invoke(this, new ReceivedDateEventArgs(data, clientSocket));
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "处理客户端时发生错误");
        }
        finally
        {
            clientSocket.SafeClose();
            ClientDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public async Task<byte[]?> SendToServer(byte[] data, bool isWaitResponse = true)
    {
        if (_isServer)
        {
            throw new Exception("服务器不能调用SendToServer方法");
        }
        await _socket.SendAsync(data);
        if (!isWaitResponse)
        {
            return null;
        }
        var buffer = new byte[1024];
        var received = await _socket.ReceiveAsync(buffer);
        var responseData = new byte[received];
        Array.Copy(buffer, responseData, received);
        return responseData;
    }
    
    ~TcpHelper()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        _ctx.Cancel();
        _socket.SafeClose();
    }
}