using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

/// <summary>
/// TCP 客户端的相关方法
/// </summary>
public class TcpClientHelper
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource _cts =  new();

    /// <summary>
    /// 连接到目标 TCP 服务器
    /// </summary>
    public async Task ConnectAsync(string ip, int port)
    {
        _tcpClient = new TcpClient();
        _cts = new CancellationTokenSource();
        
        await _tcpClient.ConnectAsync(ip, port);
        _stream = _tcpClient.GetStream();
        
        LogWrapper.Info("Net", "已连接到目标 TCP 服务器");
    }

    // 发送数据
    public async Task SendAsync(byte[] data)
    {
        if (_stream == null || !_stream.CanWrite)
            throw new InvalidOperationException("Stream not available for writing");

        // 添加数据长度前缀
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
        byte[] message = new byte[lengthPrefix.Length + data.Length];
        
        Buffer.BlockCopy(lengthPrefix, 0, message, 0, lengthPrefix.Length);
        Buffer.BlockCopy(data, 0, message, lengthPrefix.Length, data.Length);

        await _stream.WriteAsync(message, 0, message.Length, _cts.Token);
        await _stream.FlushAsync(_cts.Token);
    }

    // 接收数据
    public async Task<byte[]> ReceiveAsync()
    {
        if (_stream == null || !_stream.CanRead)
            throw new InvalidOperationException("Stream not available for reading");

        // 先读取数据长度
        byte[] lengthBuffer = new byte[4];
        int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4, _cts.Token);
        if (bytesRead != 4)
            throw new Exception("Failed to read length prefix");

        int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
        
        // 读取实际数据
        byte[] dataBuffer = new byte[dataLength];
        int totalRead = 0;
        
        while (totalRead < dataLength)
        {
            int read = await _stream.ReadAsync(dataBuffer, totalRead, dataLength - totalRead, _cts.Token);
            if (read == 0)
                throw new Exception("Connection closed while reading data");
            
            totalRead += read;
        }

        return dataBuffer;
    }

    // 发送和接收组合
    public async Task<byte[]> SendAndReceiveAsync(byte[] data)
    {
        await SendAsync(data);
        return await ReceiveAsync();
    }

    public void Disconnect()
    {
        _cts.Cancel();
        _stream?.Close();
        _tcpClient?.Close();
    }
}