using System;
using System.Linq;
using PCL.Core.Link.Lobby.Parser;
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
    /// <returns><see cref="LobbyInfo"/></returns>
    public static LobbyInfo Parse(string code)
    {
        // 获取所有实现IParser的类
        var parserTypes = typeof(IParser).GetImplements();
        var parser = parserTypes
            .Select(parserType => (IParser)Activator.CreateInstance(parserType)!)
            .FirstOrDefault(parser => parser.Validate(code));
        
        return parser != null ? parser.Parse(code) : throw new ArgumentException("无效的LobbyId");
    }
    
    /// <summary>
    /// 生成一个CE大厅
    /// </summary>
    /// <returns><see cref="LobbyInfo"/></returns>
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