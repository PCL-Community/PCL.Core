using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using PCL.Core.Logging;

namespace PCL.Core.Link
{
    public static class McPortForward
    {
        private static Thread? _udpThread, _tcpThread;
        private static Socket? _serverSocket, _boardcastClient;

        private static bool _isRunning = false;
        private static int _retryTimes = 0;
        public static int? LocalPort;

        public static async void StartAsync(string remoteIp, int remotePort, string desc = "§ePCL CE 局域网广播", bool isRetry = false)
        {
            if (_isRunning) { return; }
            if (isRetry) { _retryTimes += 1; }
            LogWrapper.Info("Link", $"开始 MC 端口转发，远程 IP: {remoteIp}, 远程端口: {remotePort}");

            IPEndPoint sip = new IPEndPoint((await Dns.GetHostAddressesAsync(remoteIp))[0], remotePort);

            _serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            _serverSocket.Listen(-1);

            IPEndPoint? endPoint = (IPEndPoint?)_serverSocket.LocalEndPoint;
            if (endPoint == null) { return; }
            LocalPort = endPoint.Port;

            _udpThread = new Thread(async () =>
            {
                try
                {
                    LogWrapper.Info("Link", $"开始进行 MC 局域网广播, 广播的本地端口: {LocalPort}");
                    _boardcastClient = new Socket(SocketType.Dgram, ProtocolType.Udp);
                    _boardcastClient.DualMode = true;
                    byte[] buffer = Encoding.UTF8.GetBytes($"[MOTD]{desc}[/MOTD][AD]{LocalPort}[/AD]");
                    var boardcastEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4445);
                    LogWrapper.Info("Link", $"端口转发: {remoteIp}:{remotePort} -> 本地 {LocalPort}");
                    while (_isRunning)
                    {
                        if (_boardcastClient != null)
                        {
                            _boardcastClient.SendTo(buffer, boardcastEndpoint);
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
                        StartAsync(remoteIp, remotePort, desc, true);
                    }
                    else
                    {
                        LogWrapper.Error(ex, "Link", "Minecraft UDP 广播线程异常");
                        _isRunning = false;
                    }
                }
            });

            _tcpThread = new Thread(async () =>
            {
                Socket c, s;
                try
                {
                    while (_isRunning)
                    {
                        LogWrapper.Info("Link", "开始等待 MC 客户端连接");
                        c = await _serverSocket.AcceptAsync();
                        s = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        LogWrapper.Info("Link", $"接受来自 {c.RemoteEndPoint} 的连接");
                        s.Connect(sip);
                        _fw_s = s;
                        _fw_c = c;
                        await _Forward(s,c);
                    }
                }
                catch (SocketException)
                {
                    if (!_isRunning) { return; }
                    LogWrapper.Info("Link", "疑似 MC 断开与创建者的连接，再次进行广播");
                    _StartUdpBoardcast();
                }
                catch (Exception ex)
                {
                    if (!_isRunning) { return; }
                    if (_retryTimes < 4)
                    {
                        LogWrapper.Warn(ex, "Link", $"Minecraft TCP 转发线程异常，放弃前再尝试 {3 - _retryTimes} 次");
                        StartAsync(remoteIp, remotePort, desc, true);
                    }
                    else
                    {
                        LogWrapper.Error(ex, "Link", "Minecraft TCP 转发线程异常");
                        _isRunning = false;
                    }
                }
            });

            try
            {
                _isRunning = true;
                _udpThread.Start();
                _tcpThread.Start();
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Link", "尝试启动 Minecraft 端口转发时遇到问题");
                _isRunning = false;
            }
        }

        private static void _StartUdpBoardcast()
        {
            try
            {
                if (_udpThread != null)
                {
                    try { _udpThread.Interrupt(); }
                    catch (Exception) { }
                    _udpThread.Start();
                }
            }
            catch (Exception ex)
            {
                LogWrapper.Warn(ex, "Link", "启动 MC 局域网广播失败");
            }
        }

        public static void Stop()
        {
            LocalPort = null;
            _isRunning = false;
            if (_udpThread != null)
            {
                _udpThread.Interrupt();
                _udpThread = null;
            }
            if (_tcpThread != null)
            {
                _tcpThread.Interrupt();
                _tcpThread = null;
            }
            if (_boardcastClient != null)
            {
                _boardcastClient.Close();
                _boardcastClient = null;
            }
            if (_serverSocket != null)
            {
                _serverSocket.Close();
                _serverSocket = null;
            }
            if (_fw_s != null)
            {
                _fw_s.Disconnect(false);
                _fw_s.Close();
                _fw_s = null;
            }
            if (_fw_c != null)
            {
                _fw_c.Disconnect(false);
                _fw_c.Close();
                _fw_c = null;
            }
        }

        private static Socket? _fw_s, _fw_c;
        private static async Task _Forward(Socket localSocket, Socket remoteSocket)
        {
            LogWrapper.Info("Link", $"开始端口转发 {localSocket.RemoteEndPoint} -> {remoteSocket.RemoteEndPoint}");
            try
            {
                Task forwardToRemote, forwardToLocal;
                using var localStream = new NetworkStream(localSocket, false);
                using var remoteStream = new NetworkStream(remoteSocket, false);
                forwardToRemote = localStream.CopyToAsync(remoteStream);
                forwardToLocal = remoteStream.CopyToAsync(localStream);
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
                catch (Exception) { } // 忽略
                try
                {
                    if (localSocket.Connected) { localSocket.Shutdown(SocketShutdown.Both); }
                    localSocket.Close();
                }
                catch (Exception) { } // 忽略
                _fw_s = null;
                _fw_c = null;
            }
        }
    }
}
