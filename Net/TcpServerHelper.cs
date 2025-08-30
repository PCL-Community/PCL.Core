using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// TCP 服务端的相关方法
/// </summary>
public class TcpServerHelper
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    
    /// <summary>
    /// 启动 TCP 服务端
    /// </summary>
    /// <param name="port">服务端监听端口</param>
    /// <param name="function">处理远端信息的委托</param>
    public async Task StartAsync(int port, Func<BinaryReader, byte[]> function)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _cts = new CancellationTokenSource();
        
        _listener.Start();
        LogWrapper.Info("Net", $"TCP 服务端已在端口 {port} 启动");

        while (!_cts.Token.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = Task.Run(() => _HandleClientAsync(client, _cts.Token, function), _cts.Token);
        }
    }
    
    private async Task _HandleClientAsync(TcpClient client, CancellationToken cancellationToken, Func<BinaryReader, byte[]> function)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            LogWrapper.Info("Net", $"接受连接: {client.Client.RemoteEndPoint}");
            try
            {
                while (client.Connected && !cancellationToken.IsCancellationRequested)
                {
                    using var reader = new BinaryReader(stream, Encoding.UTF8, true);
                    // 回复
                    var responseData = function(reader);
                    
                    await _SendDataAsync(stream, responseData, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Net", "TCP 服务端异常");
            }
        }
    }

    private static async Task _SendDataAsync(NetworkStream stream, byte[] data, CancellationToken cancellationToken)
    {
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
        var message = new byte[lengthPrefix.Length + data.Length];
        
        Buffer.BlockCopy(lengthPrefix, 0, message, 0, lengthPrefix.Length);
        Buffer.BlockCopy(data, 0, message, lengthPrefix.Length, data.Length);
        
        await stream.WriteAsync(message, 0, message.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
    }
}