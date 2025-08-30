using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public class TcpConnectionHandler
{
    public static int Port { get; set; }
    public static bool IsRunning { get; set; }
    
    public static void StartResponse(string[] args)
    {
        var bytes = new byte[1024];

        // 创建本地 IP 地址和 TCP 端口号
        var ipAddress = IPAddress.Parse("127.0.0.1");
        Port = NetworkHelper.NewTcpPort();
        LogWrapper.Info("Net", $"准备 TCP Listener, 端口 {Port}");

        // 创建 Socket
        var listener = new TcpListener(ipAddress, Port);

        try
        {
            // 启动监听器
            listener.Start();

            LogWrapper.Info("Net", "开始接受 TCP 数据流");

            // 等待一个客户端连接
            var client = listener.AcceptTcpClient();
            LogWrapper.Info("Net", "已接受到第一个 TCP 数据流");

            // 获取客户端连接网络流
            var stream = client.GetStream();

            int i;

            // 循环读取客户端发送的数据
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                var data = Encoding.UTF8.GetString(bytes, 0, i);
                LogWrapper.Debug("Net", "TCP Listener Received: " + data);

                // 将数据转换成大写并发送回客户端
                data = data.ToUpper();

                var msg = Encoding.UTF8.GetBytes(data);

                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent: {0}", data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}