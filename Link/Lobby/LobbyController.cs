using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using PCL.Core.Link.EasyTier;
using PCL.Core.Logging;
using PCL.Core.Utils.Secret;
using PCL.Core.Net;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using static PCL.Core.Link.EasyTier.ETInfoProvider;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Utils.OS;
using PCL.Core.Link.Scaffolding;
using PCL.Core.Link.Scaffolding.Client.Models;
using LobbyType = PCL.Core.Link.Scaffolding.Client.Models.LobbyType;
using PCL.Core.Link.Scaffolding.Client.Requests;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Link.Lobby;

public static class LobbyController
{
    public static ScaffoldingClientEntity? ScfClientEntity;
    public static ScaffoldingServerEntity? ScfServerEntity;

    public static ScaffoldingClientEntity? LaunchClient(string username, string code)
    {
        if (TargetLobby == null) { return null; }
        
        if (_SendTelemetry(false) == 1) { return null; }

        try
        {
            var scfEntity = ScaffoldingFactory
                .CreateClientAsync(username, code, LobbyType.Scaffolding).GetAwaiter()
                .GetResult();
            
            scfEntity.Client.ConnectAsync().GetAwaiter().GetResult();
            var port = scfEntity.Client.SendRequestAsync(new GetServerPortRequest()).GetAwaiter()
                .GetResult();

            var hostname = string.Empty;
            
            foreach (var profile in scfEntity.Client.PlayerList)
            {
                if (profile.Kind == PlayerKind.HOST)
                {
                    hostname = profile.Name;
                }
            }
            
            var desc = hostname.IsNullOrWhiteSpace() ? " - " + hostname : string.Empty;

            var tcpPortForForward = NetworkHelper.NewTcpPort();
            McForward = new TcpForward(IPAddress.Loopback, tcpPortForForward, IPAddress.Loopback, port);
            McBroadcast = new Broadcast($"§ePCL CE 大厅{desc}", tcpPortForForward);
            McForward.Start();
            McBroadcast.Start();
        
            return scfEntity;
        }
        catch (ArgumentNullException e)
        {
            LogWrapper.Error(e, "大厅创建者的用户名为空");
        }
        catch (ArgumentException e)
        {
            if (e.Message.Contains("lobby code"))
            {
                LogWrapper.Error(e, "大厅编号无效");
            }
            else if (e.Message.Contains("hostname"))
            {
                LogWrapper.Error(e, "大厅创建者的用户名无效");
            }
            else
            {
                LogWrapper.Error(e, "在加入大厅时出现意外的无效参数");
            }
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "在加入大厅时发生意外错误");
        }

        return null;
    }

    public static ScaffoldingServerEntity? LaunchServer(string username, int port)
    {
        if (_SendTelemetry(true) == 1) { return null; }
        
        return ScaffoldingFactory.CreateServer(port, username);
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
    public static async Task<int> Close()
    {
        // TargetLobby = null;
        // ETController.Exit();
        McForward?.Stop();
        McBroadcast?.Stop();
        if (ScfClientEntity != null)
        {
            await ScfClientEntity.Client.DisposeAsync();
            ScfClientEntity.EasyTier.Stop();
        } 
        else if (ScfServerEntity != null)
        {
            await ScfServerEntity.Server.DisposeAsync();
            ScfServerEntity.EasyTier.Stop();
        }
        return 0;
    }

    private static int _SendTelemetry(bool isHost)
    {
        LogWrapper.Info("Link", "开始发送联机数据");
        var servers = Config.Link.RelayServer;
        var serverType = Config.Link.ServerType;

        if (Config.Link.ServerType != 2)
        {
            servers = (
                from relay in ETRelay.RelayList
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
            ["CustomName"] = Config.Link.Username,
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

        return 0;
    }
}
