using System.Diagnostics.CodeAnalysis;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// 房间号解析器接口，用于解析各个联机服务生成的房间号
/// </summary>
public interface ILobbyIdParser
{
    /// <summary>
    /// 解析指定的房间号并输出LobbyInfo对象
    /// </summary>
    /// <param name="code">要解析的房间号</param>
    /// <param name="lobbyInfo">解析成功时输出的LobbyInfo对象，解析失败时为null</param>
    /// <returns>是否解析成功</returns>
    public bool TryParse(string code, [NotNullWhen(true)] out LobbyInfo? lobbyInfo);
}