using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using PCL.Core.Logging;
using static PCL.Core.App.Basics;

namespace PCL.Core.Link
{
    public class McPortForward
    {
        private Thread? _udpThread, _tcpThread;
        private Socket? _serverSocket, _boardcastClient;

        private bool _isRunning = false;
        private int _retryTimes = 0;

        public async void StartAsync(string remoteIp, int remotePort, string desc = "§ePCL CE 局域网广播", bool isRetry = false)
        {
            if (_isRunning) { return; }
            if (isRetry) { _retryTimes += 1; }
            LogWrapper.Info("Link", $"开始 MC 端口转发，远程 IP: {remoteIp}, 远程端口: {remotePort}");

            IPEndPoint sip = new IPEndPoint((await Dns.GetHostAddressesAsync(remoteIp))[0], remotePort);

            _serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            _serverSocket.Listen(-1);

            int localPort = ((IPEndPoint)_serverSocket.LocalEndPoint).Port;

            _udpThread = new Thread(async () =>
            {
                try
                {
                    LogWrapper.Info("Link", $"开始进行 MC 局域网广播, 广播的本地端口: {localPort}");
                    _boardcastClient = new Socket(SocketType.Dgram, ProtocolType.Udp);
                    _boardcastClient.DualMode = true;
                    byte[] buffer = Encoding.UTF8.GetBytes($"[MOTD]{desc}[/MOTD][AD]{localPort}[/AD]");
                    var boardcastEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4445);
                    LogWrapper.Info("Link", $"端口转发: {remoteIp}:{remotePort} -> 本地 {localPort}");
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
                        c = _serverSocket.Accept();
                        s = new Socket(SocketType.Stream, ProtocolType.Tcp);

                        s.Connect(sip);
                        int count = 0;
                        while (!s.Connected)
                        {
                            if (count <= 5)
                            {
                                count += 1;
                                await Task.Delay(1000);
                            }
                            else
                            {
                                LogWrapper.Warn("Link", "连接到目标 MC 服务器失败");
                                return;
                            }
                            RunInNewThread(() => _Forward(c, s));
                            RunInNewThread(() => _Forward(s,c));
                        }
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

        private void _StartUdpBoardcast()
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

        public void Stop()
        {
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

        private Socket? _fw_s, _fw_c;
        private void _Forward(Socket s, Socket c)
        {
            _fw_s = s;
            _fw_c = c;
            try
            {
                byte[] buffer = new byte[8192];
                while (_isRunning)
                {
                    int count = s.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                    if (count > 0)
                    {
                        c.Send(buffer, 0, count, SocketFlags.None);
                    }
                    else
                    {
                        _fw_s = null;
                        _fw_c = null;
                        break;
                    }
                }
            }
            catch (Exception)
            {
                try { c.Disconnect(false); } catch (Exception) { }
                try { c.Disconnect(false); } catch (Exception) { }
            }
        }
    }
}
