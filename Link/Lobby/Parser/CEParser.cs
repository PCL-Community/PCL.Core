using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// PCLCE启动器的LobbyId解析器，用于验证和解析PCLCE启动器生成的LobbyId
/// </summary>
public class CEParser : IParser
{
    /// <summary>
    /// 验证指定的代码是否为有效的PCLCE房间号
    /// </summary>
    /// <param name="code">待验证的房间号</param>
    /// <returns>元组，包含isValid指示代码是否有效，以及error包含错误信息（如果无效）</returns>
    public (bool isValid, string? error) Validate(string code)
    {
        // 检查代码长度是否正确，PCLCE房间号应为10位
        if (code.Length != 10)
        {
            return (false, "长度不正确，应为10位");
        }
        
        try
        {
            // 尝试将Base32编码的代码转换为十进制字符串
            _ = code.FromB32ToB10();
            return (true, null);
        }
        catch
        {
            return (false, "包含无效字符，无法解析为Base32编码");
        }
    }

    /// <summary>
    /// 解析指定的PCLCE房间号并转换为LobbyInfo对象
    /// </summary>
    /// <param name="code">要解析的PCLCE房间号</param>
    /// <returns>解析后的LobbyInfo对象</returns>
    public LobbyInfo Parse(string code)
    {
        // 将Base32编码的代码转换为十进制字符串
        var info = code.FromB32ToB10();
        
        return new LobbyInfo
        {
            OriginalCode = code,
            NetworkName = info[..8],
            NetworkSecret = info[8..10],
            Port = int.Parse(info[10..]),
            Type = LobbyType.PCLCE,
            Ip = "10.114.51.41"
        };
    }
}