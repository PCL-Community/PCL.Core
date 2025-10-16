using System;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// PCLCE启动器的LobbyId解析器，用于验证和解析PCLCE启动器生成的LobbyId
/// </summary>
[Parser]
public class CEParser : IParser
{

    /// <inheritdoc />
    public bool TryParse(string code, out LobbyInfo? lobbyInfo)
    {
        try
        {
            // 将Base32编码的代码转换为十进制字符串
            var info = code.FromB32ToB10();

            lobbyInfo = new LobbyInfo
            {
                OriginalCode = code,
                NetworkName = info[..8],
                NetworkSecret = info[8..10],
                Port = int.Parse(info[10..]),
                Type = LobbyType.PCLCE,
                Ip = "10.114.51.41"
            };
            return true;
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Link", $"尝试解析LobbyId失败, 可能为非PCLCE启动器生成的LobbyId");
            lobbyInfo = null;
            return false;
        }
    }
}