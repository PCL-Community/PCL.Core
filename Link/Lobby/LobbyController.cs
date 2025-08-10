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
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using static PCL.Core.Utils.Secret.Identify;

namespace PCL.Core.Link.Lobby
{
    public class LobbyController
    {
        private readonly EasyTierController _easyTierController = new();

        public int Launch(bool isHost, string name, string secret, int port)
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
                ["Id"] = RawCode, // TODO: 识别码
                ["NaidId"] = NaidProfile.Id,
                ["NaidEmail"] = NaidProfile.Email,
                ["NaidLastIp"] = NaidProfile.LastIp,
                ["NetworkName"] = name,
                ["Servers"] = servers,
                ["IsHost"] = isHost
            };
            JsonObject sendData = new() { ["data"] = data };
            try
            {
                string result = ""; // TODO: 发送数据
                if (result.Contains("数据已成功保存"))
                {
                    LogWrapper.Info("Link", "联机数据已发送");
                }
                else
                {
                    LogWrapper.Error("Link", "联机数据发送失败，原始返回内容: " + result);
                    // hint
                    return 1;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("429"))
                {
                    LogWrapper.Error("Link", "联机数据发送失败，请求过于频繁");
                    // hint
                }
                else
                {
                    LogWrapper.Error(ex, "Link", "联机数据发送失败");
                    // hint
                }
                return 1;
            }
            return _easyTierController.Launch(isHost, name, secret, port: port);
        }
    }
}
