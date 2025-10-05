using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Parser;

/// <summary>
/// 管理器，用于解析和生成lobby信息
/// </summary>
public interface IParser
{
    protected bool Validate(string code);
    public LobbyInfo Parse(string code);
    public string Generate(LobbyInfo info);
}