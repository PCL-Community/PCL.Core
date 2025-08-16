using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using PCL.Core.Link.EasyTier;
using PCL.Core.Logging;
using PCL.Core.ProgramSetup;
using PCL.Core.Utils.Secret;
using PCL.Core.Net;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using System.Net.Http;

namespace PCL.Core.Link.Lobby
{
    public class LobbyController
    {
        private readonly EasyTierController _easyTierController = new();
        private McPortForward _mcPortForward = new();

        public int Launch(bool isHost, LobbyInfo lobbyInfo, string? boardcastDesc = null)
        {
            LogWrapper.Info("Link", "开始发送联机数据");
            string? servers = Setup.Link.RelayServer;
            if (Setup.Link.ServerType != 2)
            {
                foreach (var relay in EasyTierRelay.RelayList)
                {
                    int serverType = Setup.Link.ServerType;
                    if ((relay.Type == EasyTierRelay.ETRelayType.Selfhosted && serverType != 2) || (relay.Type == EasyTierRelay.ETRelayType.Community && serverType == 1))
                    {
                        servers += $"{relay.Url};";
                    }
                }
            }
            JsonObject data = new()
            {
                ["Tag"] = "Link",
                ["Id"] = Identify.LaunchId,
                ["NaidId"] = NaidProfile.Id,
                ["NaidEmail"] = NaidProfile.Email,
                ["NaidLastIp"] = NaidProfile.LastIp,
                ["NetworkName"] = lobbyInfo.NetworkName,
                ["Servers"] = servers,
                ["IsHost"] = isHost
            };
            JsonObject sendData = new() { ["data"] = data };
            try
            {
                HttpContent httpContent = new StringContent(sendData.ToJsonString(), Encoding.UTF8, "application/json");
                string? result = HttpRequestBuilder.Create("https://pcl2ce.pysio.online/post", HttpMethod.Post)
                    .WithContent(httpContent).Build().Result.GetResponse().Content.ToString();
                if (result == null)
                {
                    throw new Exception("联机数据发送失败，返回内容为空");
                }
                else
                {
                    if (result.Contains("数据已成功保存"))
                    {
                        LogWrapper.Info("Link", "联机数据已发送");
                    }
                    else
                    {
                        throw new Exception("联机数据发送失败，原始返回内容: " + result);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("429"))
                {
                    throw new Exception("联机数据发送失败，请求过于频繁", ex);
                }
                else
                {
                    throw new Exception("联机数据发送失败", ex);
                }
            }

            _easyTierController.Launch(isHost, lobbyInfo.NetworkName, lobbyInfo.NetworkSecret, port: lobbyInfo.Port);
            // if (!isHost && lobbyInfo.Ip != null) { _mcPortForward.StartAsync(lobbyInfo.Ip, lobbyInfo.Port, "§ePCL CE 大厅" + (boardcastDesc != null, " - " + boardcastDesc)); }

            return 0;
        }

        /// <summary>
        /// 关闭 EasyTier 和 MC 端口转发，需要自行清理 UI。
        /// </summary>
        /// <returns></returns>
        public int Close()
        {
            _easyTierController.Exit();
            _mcPortForward.Stop();
            return 0;
        }
    }
}
