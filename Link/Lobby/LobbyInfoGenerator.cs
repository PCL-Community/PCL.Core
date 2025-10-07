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
    /// <exception cref="ArgumentException">解析错误时抛出</exception>
    public static LobbyInfo Parse(string code)
    {
        try
        {
            var parsers = ParserRegistry.ParserTypes
                .Select(parserType => (IParser)Activator.CreateInstance(parserType)!);
            var parser = parsers
                .FirstOrDefault(parser =>
                {
                    var result = parser.Validate(code);
                    if (result.isValid) return true;

                    LogWrapper.Warn("Link", $"使用{ parser.GetType().Name}解析LobbyId失败: {result.error}");
                    return false;
                });

            return parser?.Parse(code) ?? throw new ArgumentException("无效的LobbyId");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Link", "解析LobbyId时发生异常");
            throw new ArgumentException("无效的LobbyId", ex);
        }
    }
    
    /// <summary>
    /// 生成一个CE大厅
    /// </summary>
    /// <returns>返回一个<see cref="LobbyInfo"/></returns>
    public static LobbyInfo Generate()
    {
        var id = RandomUtils.NextInt(10000000, 99999999).ToString();
        var secret = RandomUtils.NextInt(10, 99).ToString();
        var port = NetworkHelper.NewTcpPort();
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