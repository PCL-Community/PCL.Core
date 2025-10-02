using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public class TcpHelper : IDisposable
{
    /// <summary>
    /// 是否为服务器, 用于判断使用哪种方法
    /// </summary>
    private bool _isServer;
    private bool _isRunning;
    private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private readonly CancellationTokenSource _ctx = new CancellationTokenSource();
    
    public class ReceivedDateEventArgs(byte[] data) : EventArgs
    {
        /// <summary>
        /// 接收到的数据
        /// </summary>
        public byte[] Data => data;
        /// <summary>
        /// 服务端返回数据
        /// </summary>
        public byte[]? Response { get; set; }
    }
    
    public event EventHandler<ReceivedDateEventArgs>? ReceivedData;
    
    public void StartListening(int port)
    {
        try
        {
            if (_isRunning)
            {
                LogWrapper.Warn("TCP", "服务已在运行中");
            }
            _isServer = true;
            _isRunning = true;
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            _ = Task.Run(() => _AcceptConnections(_ctx.Token), _ctx.Token);
            LogWrapper.Info($"TCP服务已启动, 监听端口 {port}");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "启动TCP服务时发生错误");
        }
    }
    
    public void Connect(string ip, int port)
    { 
        try
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("服务已在运行中");
            }
            _isServer = false;
            _isRunning = true;
            _socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            LogWrapper.Info($"TCP服务已启动, 连接到 {ip}:{port}");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "启动TCP服务时发生错误");
        }
    }
    
    private async Task _AcceptConnections(CancellationToken token)
    {
        try
        {
            if (!_isRunning)
            {
                throw new InvalidOperationException("服务未启动");
            }
            if (!_isServer)
            {
                throw new InvalidOperationException("客户端不能调用AcceptConnections方法");
            }
            _socket.Listen(10);
            while (!token.IsCancellationRequested)
            {
                var clientSocket = await _socket.AcceptAsync(token);
                _ = Task.Run(() => _HandleClientAsync(clientSocket, token), token);
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "接受客户端连接时发生错误");
        }
    }

    private async Task _HandleClientAsync(Socket clientSocket, CancellationToken token)
    {
        if (clientSocket.RemoteEndPoint == null)
        {
            throw new ArgumentNullException("无法获取客户端地址");
        }
        LogWrapper.Info("TCP", $"接受来自 {clientSocket.RemoteEndPoint} 的连接");
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
                
                var receivedDateEventArgs = new ReceivedDateEventArgs(data);
                ReceivedData?.Invoke(this, receivedDateEventArgs);
                if (receivedDateEventArgs.Response != null)
                {
                    await clientSocket.SendAsync(receivedDateEventArgs.Response);
                }
            }
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "处理客户端时发生错误");
        }
        finally
        {
            clientSocket.SafeClose();
        }
    }
    
    public async Task<byte[]?> SendToServerAsync(byte[] data, bool isWaitResponse = true)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("服务未启动");
        }
        if (_isServer)
        {
            throw new InvalidOperationException("服务器不能调用 SendToServerAsync 方法");
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
    
    public int Close()
    {
        try
        {
            _ctx.Cancel();
            _socket.SafeClose();
            LogWrapper.Info($"TCP服务已关闭");
            return 0;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "TCP", "关闭TCP服务时发生错误");
            return 1;
        }
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
        Close();
    }
}