using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;
public class TcpForward(
    IPAddress listenAddress,
    int listenPort,
    IPAddress targetAddress,
    int targetPort,
    int maxConnections = 10)
    : IDisposable
{
    private Socket? _listenerSocket;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _connectionSemaphore = new(maxConnections, maxConnections);
    private readonly ConcurrentDictionary<Guid, ConnectionPair> _activeConnections = new();

    private bool _isRunning;

    public int LocalPort { get; private set; }

    public int ActiveConnections => _activeConnections.Count;

    public void Start()
    {
        if (_isRunning) return;

        _cts = new CancellationTokenSource();
        _isRunning = true;

        try
        {
            // 创建并启动监听 Socket
            _listenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true, // 禁用 Nagle 算法以提高响应速度
                ReceiveBufferSize = 8192,
                SendBufferSize = 8192
            };

            _listenerSocket.Bind(new IPEndPoint(listenAddress, listenPort));
            _listenerSocket.Listen(100); // 设置挂起连接队列的最大长度

            if (_listenerSocket.LocalEndPoint is not IPEndPoint endPoint) throw new InvalidCastException("出现了意外的转换操作");
            LocalPort = endPoint.Port;

            // 启动 TCP 接受连接任务
            _ = Task.Run(() => _AcceptConnections(_cts.Token), _cts.Token);

            LogWrapper.Info("TcpForward", $"MC 端口转发已启动，监听 {listenAddress}:{LocalPort}，目标 {targetAddress}:{targetPort}");
        }
        catch (Exception ex)
        {
            _isRunning = false;
            LogWrapper.Error(ex, "TcpForward",  $"启动 MC 端口转发时发生错误: {ex.Message}");
            throw;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        _isRunning = false;

        // 关闭所有活动连接
        foreach (var connection in _activeConnections.Values)
        {
            connection.ClientSocket.SafeClose();
            connection.TargetSocket.SafeClose();
        }
        _activeConnections.Clear();

        _listenerSocket?.SafeClose();

        LogWrapper.Info("TcpForward", "MC 端口转发已停止");
    }

    private async Task _AcceptConnections(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() =>
        {
            _listenerSocket.SafeClose();
        });
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_listenerSocket == null) break;
                var clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);

                // 检查是否达到最大连接限制
                if (_activeConnections.Count >= maxConnections)
                {
                    clientSocket.SafeClose();
                    LogWrapper.Warn("TcpForward", $"已达到最大连接数限制({maxConnections})，拒绝新连接");
                    continue;
                }

                // 使用信号量控制并发处理
                await _connectionSemaphore.WaitAsync(cancellationToken);

                // 异步处理连接，不等待完成
                _ = Task.Run(() => _HandleConnection(clientSocket, cancellationToken), cancellationToken)
                    .ContinueWith(_ => _connectionSemaphore.Release(), TaskContinuationOptions.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "TcpForward", $"接受连接时发生错误");
                await Task.Delay(1000, cancellationToken); // 出错后等待 1 秒再继续
            }
        }
    }

    private async Task _HandleConnection(Socket clientSocket, CancellationToken cancellationToken)
    {
        var connectionId = Guid.NewGuid();

        try
        {
            LogWrapper.Info("TcpForward", $"接受来自 {clientSocket.RemoteEndPoint} 的连接");

            // 连接到目标服务器
            var targetSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                ReceiveBufferSize = 8192,
                SendBufferSize = 8192
            };

            await targetSocket.ConnectAsync(targetAddress, targetPort, cancellationToken);

            // 保存连接对
            var connectionPair = new ConnectionPair(clientSocket, targetSocket);
            _activeConnections[connectionId] = connectionPair;

            LogWrapper.Info("TcpForward", $"开始端口转发 {clientSocket.RemoteEndPoint} <-> {targetSocket.RemoteEndPoint}({connectionId})");

            // 使用高性能的 SocketAsyncEventArgs 进行双向转发
            var forwardTask1 = _ForwardDataAsync(clientSocket, targetSocket, cancellationToken);
            var forwardTask2 = _ForwardDataAsync(targetSocket, clientSocket, cancellationToken);

            // 等待任意一个方向的数据转发完成
            await Task.WhenAny(forwardTask1, forwardTask2);

            Console.WriteLine($"端口转发 {connectionId} 已完成");
        }
        catch (OperationCanceledException)
        {
            // 取消操作，正常退出
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理连接 {connectionId} 时发生错误: {ex.Message}");
        }
        finally
        {
            // 清理资源
            clientSocket.SafeClose();

            // 从活动连接中移除
            _activeConnections.TryRemove(connectionId, out _);
        }
    }

    private static async Task _ForwardDataAsync(Socket source, Socket destination, CancellationToken cancellationToken)
    {
        // 使用 ArrayPool 共享缓冲区以减少内存分配
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await source.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                if (bytesRead == 0) break; // 连接已关闭

                await destination.SendAsync(buffer, bytesRead, SocketFlags.None, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        if (_disposed) return;
        Stop();
        _cts?.Dispose();
        _connectionSemaphore.Dispose();
        _disposed = true;
    }

    ~TcpForward()
    {
        Dispose(false);
    }

    private class ConnectionPair(Socket clientSocket, Socket targetSocket)
    {
        public Socket ClientSocket { get; } = clientSocket;
        public Socket TargetSocket { get; } = targetSocket;
    }
}

public static class SocketExtensions
{
    public static void SafeClose(this Socket? socket)
    {
        if (socket is null) return;

        try
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            socket.Close();
        }
        catch { /* 忽略关闭时的任何错误 */ }
    }

    public static async Task<int> ReceiveAsync(this Socket socket, byte[] buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default)
    {
        // 使用 TaskCompletionSource和SocketAsyncEventArgs 实现高性能异步接收
        var tcs = new TaskCompletionSource<int>();
        var args = new SocketAsyncEventArgs();
        args.SetBuffer(buffer, 0, buffer.Length);
        args.SocketFlags = socketFlags;
        args.Completed += (_, e) =>
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    tcs.TrySetResult(e.BytesTransferred);
                    break;
                case SocketAsyncOperation.Disconnect:
                    tcs.TrySetResult(0); // 连接关闭
                    break;
                default:
                    tcs.TrySetException(new SocketException((int)e.SocketError));
                    break;
            }
        };

        // 注册取消令牌
        cancellationToken.Register(() => tcs.TrySetCanceled());

        if (!socket.ReceiveAsync(args))
        {
            // 操作同步完成
            return args.BytesTransferred;
        }

        return await tcs.Task;
    }

    public static async Task<int> SendAsync(this Socket socket, byte[] buffer, int length, SocketFlags socketFlags, CancellationToken cancellationToken = default)
    {
        // 使用 TaskCompletionSource 和 SocketAsyncEventArgs 实现高性能异步发送
        var tcs = new TaskCompletionSource<int>();
        var args = new SocketAsyncEventArgs();
        args.SetBuffer(buffer, 0, length);
        args.SocketFlags = socketFlags;
        args.Completed += (_, e) =>
        {
            if (e.SocketError == SocketError.Success)
                tcs.TrySetResult(e.BytesTransferred);
            else
                tcs.TrySetException(new SocketException((int)e.SocketError));
        };

        // 注册取消令牌
        cancellationToken.Register(() => tcs.TrySetCanceled());

        if (!socket.SendAsync(args))
        {
            // 操作同步完成
            return args.BytesTransferred;
        }

        return await tcs.Task;
    }
}