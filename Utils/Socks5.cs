using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Helper;
using PCL.Core.Utils.Logger;

namespace PCL.Core.Service
{
    public class Socks5
    {
        private const byte SocksVersion = 0x05;
        private const byte NoAuth = 0x00;
        private const byte UsernamePassword = 0x02;
        private const byte RejectAuth = 0xFF;
        
        private static string _easytierAddress = "";
        private static int _easytierPort = 0;
        private static TcpClient? _easyTierConnect;
        private static TcpListener _listener = new(IPAddress.Loopback, 0);
        private static CancellationTokenSource? _tokenSource;
        private static readonly object EasytierLock = new();
        public static bool EasyTierAvailable
        {
            get
            {
                return _easytierAvailable;
            }
            set
            {
                lock (EasytierLock)
                {
                    _easytierAvailable = value;
                }
            }
        }
        private static bool _easytierAvailable = false;
        

        public static async Task InitSocks5Server(string host, int port)
        {
            _easytierAddress = host;
            _easytierPort = port;
            _listener.Start();
            EasyTierAvailable = true;
            await Task.Run(async() =>
            {
                await OnConnect(_listener.AcceptTcpClient());
            });

        }
        
        public static async Task OnConnect(TcpClient client)
        {
            await Task.Run(() =>
            {
                using (NetworkStream stream = client.GetStream())
                {
                    if (_easyTierConnect is null)
                    {
                        try
                        {
                            _easyTierConnect = new TcpClient();
                            _easyTierConnect.Connect(_easytierAddress, _easytierPort);
                        }
                        catch (Exception ex)
                        {
                            LogWrapper.Warn(ex,"[Network] 未能建立到 EasyTier 的连接");
                            EasyTierAvailable = false;
                        }
                    }

                    if (_easytierAvailable)
                    {
                        using (NetworkStream stream2 = _easyTierConnect!.GetStream())
                        {
                            stream.CopyTo(stream2);
                        }

                        return;
                    }

                    using (TcpClient NetClient = new())
                    {
                        NetClient.Connect(_easytierAddress, _easytierPort);
                        using (NetworkStream transferStream = NetClient.GetStream())
                        {
                            stream.CopyTo(transferStream);
                        }
                    }
                }
            });
        }
        
    }
}
