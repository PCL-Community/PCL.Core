using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// LobbyId解析器接口，用于解析各个启动器生成的LobbyId
/// </summary>
public interface IParser
{
    public bool Validate(string code);
    public LobbyInfo Parse(string code);
}