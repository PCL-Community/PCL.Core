using System;
using System.Linq;
using PCL.Core.Link.Lobby.Parser;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby;

/// <summary>
/// LobbyInfo生成器，可用于解析和生成LobbyInfo
/// </summary>
public static class LobbyInfoGenerator
{
    /// <summary>
    /// 解析一个LobbyId
    /// </summary>
    /// <param name="code">LobbyId</param>
    /// <returns>返回一个<see cref="LobbyInfo"/></returns>
    public static LobbyInfo? Parse(string code)
    {
        try
        {
            foreach (var parser in LinkUsings.Parsers)
            {
                if (parser.TryParse(code, out var lobbyInfo))
                {
                    return lobbyInfo;
                }
            }
            
            LogWrapper.Warn("Link", $"无法解析LobbyId, 可能为无效或无法识别的房间号");
            return null;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Link", "解析房间号时发生异常");
            return null;
        }
    }
    
    /// <summary>
    /// 生成一个CE大厅
    /// </summary>
    /// <returns>返回一个<see cref="LobbyInfo"/></returns>
    public static LobbyInfo Generate(int port)
    {
        var id = RandomUtils.NextInt(10000000, 99999999).ToString();
        var secret = RandomUtils.NextInt(10, 99).ToString();
        return new LobbyInfo
        {
            NetworkName = id,
            NetworkSecret = secret,
            OriginalCode = $"{id}{secret}{port}".FromB10ToB32(),
            Type = LobbyType.PCLCE,
            Port = port
        };
    }
}