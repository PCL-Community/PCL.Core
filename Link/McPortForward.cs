using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using PCL.Core.Logging;

namespace PCL.Core.Link;

public static class McPortForward
{
    private static Task? _udpTask, _tcpTask;
    private static CancellationTokenSource? _cts, _udpCts, _tcpCts;
    private static Socket? _serverSocket, _broadcastClient;

    private static bool _isRunning = false;
    private static int _retryTimes = 0;
    public static int? LocalPort;

    public static async Task StartAsync(string remoteIp, int remotePort, string desc = "§ePCL CE 局域网广播", bool isRetry = false)
    {
        if (_isRunning) { return; }
        if (isRetry) { _retryTimes += 1; }
        LogWrapper.Info("Link", $"开始 MC 端口转发，远程 IP: {remoteIp}, 远程端口: {remotePort}");

        var sip = new IPEndPoint((await Dns.GetHostAddressesAsync(remoteIp))[0], remotePort);

        _serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
        _serverSocket.Listen(-1);

        var endPoint = (IPEndPoint?)_serverSocket.LocalEndPoint;
        if (endPoint == null) { return; }
        LocalPort = endPoint.Port;
        _cts = new CancellationTokenSource();

        if (_udpTask != null) _udpCts?.Cancel();
        _udpTask = new Task(async void () =>
        {
            try
            {
                LogWrapper.Info("Link", $"开始进行 MC 局域网广播, 广播的本地端口: {LocalPort}");
                _broadcastClient = new Socket(SocketType.Dgram, ProtocolType.Udp);
                _broadcastClient.DualMode = true;
                var buffer = Encoding.UTF8.GetBytes($"[MOTD]{desc}[/MOTD][AD]{LocalPort}[/AD]");
                var broadcastEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4445);
                LogWrapper.Info("Link", $"端口转发: {remoteIp}:{remotePort} -> 本地 {LocalPort}");
                while (_isRunning)
                {
                    if (_broadcastClient != null)
                    {
                        _broadcastClient.SendTo(buffer, broadcastEndpoint);
                        await Task.Delay(1500);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isRunning) { return; }
                if (_retryTimes < 4)
                {
                    LogWrapper.Warn(ex, "Link", $"Minecraft UDP 广播线程异常，放弃前再尝试 {3 - _retryTimes} 次");
                    await StartAsync(remoteIp, remotePort, desc, true);
                }
                else
                {
                    LogWrapper.Error(ex, "Link", "Minecraft UDP 广播线程异常");
                    _isRunning = false;
                }
            }
        }, (_udpCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token)).Token);

        if (_tcpTask != null) _tcpCts?.Cancel();
        _tcpTask = new Task(async void () =>
        {
            try
            {
                while (_isRunning)
                {
                    LogWrapper.Info("Link", "开始等待 MC 客户端连接");
                    var c = await _serverSocket.AcceptAsync();
                    var s = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    LogWrapper.Info("Link", $"接受来自 {c.RemoteEndPoint} 的连接");
                    s.Connect(sip);
                    _fwS = s;
                    _fwC = c;
                    await _Forward(s,c);
                }
            }
            catch (Exception ex)
            {
                if (!_isRunning) { return; }
                if (_retryTimes < 4)
                {
                    LogWrapper.Warn(ex, "Link", $"Minecraft TCP 转发线程异常，放弃前再尝试 {3 - _retryTimes} 次");
                    await StartAsync(remoteIp, remotePort, desc, true);
                }
                else
                {
                    LogWrapper.Error(ex, "Link", "Minecraft TCP 转发线程异常");
                    _isRunning = false;
                }
            }
        }, (_tcpCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token)).Token);

        try
        {
            _isRunning = true;
            _udpTask.Start();
            _tcpTask.Start();
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Link", "尝试启动 Minecraft 端口转发时遇到问题");
            _isRunning = false;
        }
    }

    public static void Stop()
    {
        LocalPort = null;
        _isRunning = false;
        _cts?.Cancel();
        if (_broadcastClient != null)
        {
            _broadcastClient.Close();
            _broadcastClient = null;
        }
        if (_serverSocket != null)
        {
            _serverSocket.Close();
            _serverSocket = null;
        }
        if (_fwS != null)
        {
            _fwS.Disconnect(false);
            _fwS.Close();
            _fwS = null;
        }
        if (_fwC != null)
        {
            _fwC.Disconnect(false);
            _fwC.Close();
            _fwC = null;
        }
    }

    private static Socket? _fwS, _fwC;
    private static async Task _Forward(Socket localSocket, Socket remoteSocket)
    {
        LogWrapper.Info("Link", $"开始端口转发 {localSocket.RemoteEndPoint} -> {remoteSocket.RemoteEndPoint}");
        try
        {
            using var localStream = new NetworkStream(localSocket, false);
            using var remoteStream = new NetworkStream(remoteSocket, false);
            var forwardToLocal = remoteStream.CopyToAsync(localStream);
            var forwardToRemote = localStream.CopyToAsync(remoteStream);
            await Task.WhenAny(forwardToLocal, forwardToRemote);
            await Task.Delay(500);
            LogWrapper.Info("Link", $"端口转发任务 {localSocket.RemoteEndPoint} -> {remoteSocket.RemoteEndPoint} 已结束");
        }
        catch (ObjectDisposedException)
        {
            LogWrapper.Info("Link", "端口转发流已释放，正常结束");
        }
        catch (IOException)
        {
            LogWrapper.Warn("Link", "端口转发流 IO 异常，网络可能中断");
        }
        catch (SocketException ex)
        {
            LogWrapper.Warn("Link", "端口转发 Socket 异常: " + ex.Message);
        }
        catch (Exception)
        {
            LogWrapper.Error("Link", "端口转发过程中发生意外异常");
            throw;
        }
        finally
        {
            try
            {
                if (remoteSocket.Connected) { remoteSocket.Shutdown(SocketShutdown.Both); }
                remoteSocket.Close();
            }
            catch (Exception) { /* 忽略 */ }
            try
            {
                if (localSocket.Connected) { localSocket.Shutdown(SocketShutdown.Both); }
                localSocket.Close();
            }
            catch (Exception) { /* 忽略 */ }
            _fwS = null;
            _fwC = null;
        }
    }
}
