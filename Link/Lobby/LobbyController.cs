using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;

using PCL.Core.Link.EasyTier;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils.Secret;
using PCL.Core.Net;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using static PCL.Core.Link.EasyTier.ETInfoProvider;
using System.Threading.Tasks;
using PCL.Core.Utils.OS;

namespace PCL.Core.Link.Lobby;

public static class LobbyController
{
    public static int Launch(bool isHost, LobbyInfo lobbyInfo, string? playerName = null)
    {
        LogWrapper.Info("Link", "开始发送联机数据");
        var servers = Setup.Link.RelayServer;
        if (Setup.Link.ServerType != 2)
        {
            servers = (
                from relay in ETRelay.RelayList
                let serverType = Setup.Link.ServerType
                where (relay.Type == ETRelayType.Selfhosted && serverType != 2) || (relay.Type == ETRelayType.Community && serverType == 1)
                select relay
            ).Aggregate(servers, (current, relay) => current + $"{relay.Url};");
        }
        JsonObject data = new()
        {
            ["Tag"] = "Link",
            ["Id"] = Identify.LaunchId,
            ["NaidId"] = NaidProfile.Id,
            ["NaidEmail"] = NaidProfile.Email,
            ["NaidLastIp"] = NaidProfile.LastIp,
            ["CustomName"] = Setup.Link.Username,
            ["NetworkName"] = lobbyInfo.NetworkName,
            ["Servers"] = servers,
            ["IsHost"] = isHost
        };
        JsonObject sendData = new() { ["data"] = data };
        try
        {
            HttpContent httpContent = new StringContent(sendData.ToJsonString(), Encoding.UTF8, "application/json");
            var key = EnvironmentInterop.GetSecret("TelemetryKey");
            if (key == null)
            {
                if (RequiresLogin)
                {
                    LogWrapper.Error("Link", "联机数据发送失败，未设置 TelemetryKey");
                    return 1;
                }
                LogWrapper.Warn("Link", "联机数据发送失败，未设置 TelemetryKey，跳过发送");
            }
            else
            {
                using var response = HttpRequestBuilder
                    .Create("https://pcl2ce.pysio.online/post", HttpMethod.Post)
                    .WithContent(httpContent)
                    .WithAuthentication(key)
                    .SendAsync().Result;
                if (!response.IsSuccess)
                {
                    if (RequiresLogin)
                    {
                        LogWrapper.Error("Link", "联机数据发送失败，响应内容为空");
                        return 1;
                    }
                    LogWrapper.Warn("Link", "联机数据发送失败，响应内容为空，跳过发送");
                }
                else
                {
                    var result = response.AsStringAsync().Result;
                    if (result.Contains("数据已成功保存"))
                    {
                        LogWrapper.Info("Link", "联机数据已发送");
                    }
                    else
                    {
                        if (RequiresLogin)
                        {
                            LogWrapper.Error("Link", "联机数据发送失败，响应内容: " + result);
                            return 1;
                        }
                        LogWrapper.Warn("Link", "联机数据发送失败，跳过发送，响应内容: " + result);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (RequiresLogin)
            {
                LogWrapper.Error(ex, "Link",
                    ex.Message.Contains("429") ? "联机数据发送失败，请求过于频繁" : "联机数据发送失败");
                return 1;
            }
            LogWrapper.Warn(ex, "Link", "联机数据发送失败，跳过发送");
        }

        var etResult = ETController.Launch(isHost, lobbyInfo.NetworkName, lobbyInfo.NetworkSecret, port: lobbyInfo.Port, hostname: playerName);
        if (etResult == 1)
        {
            return 1;
        }
            
        while (CheckETStatus().GetAwaiter().GetResult() != 0)
        {
            Task.Delay(800).GetAwaiter().GetResult();
        }

        if (isHost || lobbyInfo.Ip is null) return 0;
        string desc;
        var hostInfo = GetPlayerList().Item1?[0];
        if (hostInfo == null)
        {
            desc = string.Empty;
        }
        else
        {
            desc = " - " + (hostInfo.Username ?? hostInfo.Hostname);
        }

        var tcpPortForForward = NetworkHelper.NewTcpPort();
        McForward = new TcpForward(IPAddress.Loopback, tcpPortForForward, IPAddress.Loopback,
            JoinerLocalPort);
        McBroadcast = new Broadcast($"§ePCL CE 大厅{desc}", tcpPortForForward);
        McForward.Start();
        McBroadcast.Start();
        
        return 0;
    }

    /// <summary>
    /// 检查主机的 MC 实例是否可用。
    /// </summary>
    public static bool IsHostInstanceAvailable(int port)
    {
        var ping = new McPing("127.0.0.1", port);
        var info = ping.PingAsync().GetAwaiter().GetResult();
        if (info != null) return true;
        LogWrapper.Warn("Link", $"本地 MC 局域网实例 ({port}) 疑似已关闭");
        return false;
    }

    /// <summary>
    /// 退出大厅。这将同时关闭 EasyTier 和 MC 端口转发，需要自行清理 UI。
    /// </summary>
    public static int Close()
    {
        TargetLobby = null;
        ETController.Exit();
        McForward?.Stop();
        McBroadcast?.Stop();
        return 0;
    }
}
