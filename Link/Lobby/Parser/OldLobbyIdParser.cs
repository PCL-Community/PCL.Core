using System;
using System.Diagnostics.CodeAnalysis;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// PCLCE 启动器的房间号解析器，用于验证和解析 PCLCE 启动器生成的房间号
/// </summary>
public class OldLobbyIdParser : ILobbyIdParser
{
    /// <inheritdoc />
    public bool TryParse(string code, [NotNullWhen(true)] out LobbyInfo? lobbyInfo)
    {
        if (code.Length != 10)
        {
            LogWrapper.Warn("Link", $"尝试解析LobbyId失败, 输入长度错误 (旧版房间号)");
            lobbyInfo = null;
            return false;
        }
        try
        {
            // 将Base32编码的代码转换为十进制字符串
            var info = code.FromB32ToB10();

            lobbyInfo = new LobbyInfo(code, info[..8], info[8..10]);
            return true;
        }
        catch (Exception ex)
        {
            LogWrapper.Warn(ex, "Link", $"尝试解析房间号失败 (旧版房间号)");
            lobbyInfo = null;
            return false;
        }
    }
}