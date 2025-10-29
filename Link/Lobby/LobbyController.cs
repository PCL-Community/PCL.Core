using PCL.Core.App;
using PCL.Core.Link.EasyTier;
using PCL.Core.Link.Scaffolding;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.Client.Requests;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.OS;
using PCL.Core.Utils.Secret;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using LobbyType = PCL.Core.Link.Scaffolding.Client.Models.LobbyType;

namespace PCL.Core.Link.Lobby;

public static class LobbyController
{
    public static bool IsHost = false;
    public static ScaffoldingClientEntity? ScfClientEntity;
    public static ScaffoldingServerEntity? ScfServerEntity;

    public static async Task<ScaffoldingClientEntity?> LaunchClientAsync(string username, string code)
    {
        if (await _SendTelemetryAsync(false).ConfigureAwait(false) == 1)
        {
            return null;
        }

        try
        {
            var scfEntity = await ScaffoldingFactory
                .CreateClientAsync(username, code, LobbyType.Scaffolding).ConfigureAwait(false);

            await scfEntity.Client.ConnectAsync().ConfigureAwait(false);
            var port = scfEntity.Client.SendRequestAsync(new GetServerPortRequest()).GetAwaiter()
                .GetResult();

            var hostname = string.Empty;

            while (scfEntity.Client.PlayerList == null)
            {
                await Task.Delay(800).ConfigureAwait(false);
            }

            foreach (var profile in scfEntity.Client.PlayerList)
            {
                if (profile.Kind == PlayerKind.HOST)
                {
                    hostname = profile.Name;
                }
            }

            var localPort = scfEntity.EasyTier.AddPortForwardAsync(scfEntity.HostInfo.Ip, port).GetAwaiter()
                .GetResult();
            var desc = hostname.IsNullOrWhiteSpace() ? " - " + hostname : string.Empty;

            var tcpPortForForward = NetworkHelper.NewTcpPort();
            McForward = new TcpForward(IPAddress.Loopback, tcpPortForForward, IPAddress.Loopback, localPort);
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

    public async static Task<ScaffoldingServerEntity?> LaunchServerAsync(string username, int port)
    {
        if (await _SendTelemetryAsync(true).ConfigureAwait(false) == 1)
        {
            return null;
        }

        return ScaffoldingFactory.CreateServer(port, username);
    }

    /// <summary>
    /// 检查主机的 MC 实例是否可用。
    /// </summary>
    public static async Task<bool> IsHostInstanceAvailableAsync(int port)
    {
        var ping = new McPing("127.0.0.1", port);
        var info = await ping.PingAsync().ConfigureAwait(false);
        if (info != null) return true;
        LogWrapper.Warn("Link", $"本地 MC 局域网实例 ({port}) 疑似已关闭");
        return false;
    }

    /// <summary>
    /// 退出大厅。这将同时关闭 EasyTier 和 MC 端口转发，需要自行清理 UI。
    /// </summary>
    public static async Task<int> CloseAsync()
    {
        // TargetLobby = null;
        // ETController.Exit();
        McForward?.Stop();
        McBroadcast?.Stop();
        if (ScfClientEntity != null)
        {
            ScfClientEntity.EasyTier.Stop();
            await ScfClientEntity.Client.DisposeAsync().ConfigureAwait(false);
        }
        else if (ScfServerEntity != null)
        {
            ScfServerEntity.EasyTier.Stop();
            await ScfServerEntity.Server.DisposeAsync().ConfigureAwait(false);
        }
        return 0;
    }

    private static async Task<int> _SendTelemetryAsync(bool isHost)
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
                using var response = await HttpRequestBuilder
                    .Create("https://pcl2ce.pysio.online/post", HttpMethod.Post)
                    .WithContent(httpContent)
                    .WithAuthentication(key)
                    .SendAsync().ConfigureAwait(false);

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
                    var result = await response.AsStringAsync().ConfigureAwait(false);
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
