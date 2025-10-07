using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// 房间号解析器接口，用于解析各个联机服务生成的房间号
/// </summary>
public interface IParser
{ 
    /// <summary>
    /// 验证指定的代码是否为有效的房间号
    /// </summary>
    /// <param name="code">待验证的房间号</param>
    /// <returns>元组，包含isValid指示代码是否有效，以及error包含错误信息(如果无效)</returns>
    public (bool isValid, string? error) Validate(string code);
    
    /// <summary>
    /// 解析指定的房间号并转换为LobbyInfo对象
    /// </summary>
    /// <param name="code">要解析的房间号</param>
    /// <returns>解析后的LobbyInfo对象</returns>
    public LobbyInfo Parse(string code);
}