using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Link.Lobby.Parser;
using PCL.Core.Logging;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Lobby;

/// <summary>
/// LobbyInfo生成器，可用于解析和生成LobbyInfo
/// </summary>
public static class LobbyInfoGenerator
{
    private const string Chars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string FullCodePrefix = "U/";
    private const string NetworkNamePrefix = "scaffolding-mc-";
    private const int CodeLength = 16;
    /// <summary>
    /// 房间号解析器列表
    /// </summary>
    private static readonly IReadOnlyList<ILobbyIdParser> _Parsers = [
        new OldLobbyIdParser(),
        new LobbyIdParser()
    ];
    
    /// <summary>
    /// 解析一个LobbyId
    /// </summary>
    /// <param name="code">LobbyId</param>
    /// <returns>返回一个<see cref="LobbyInfo"/></returns>
    public static LobbyInfo? Parse(string code)
    {
        try
        {
            foreach (var parser in _Parsers)
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
    public static LobbyInfo Generate()
    {
        var randomValue = _GetSecureRandomUInt128();
        var remainder = randomValue % 7;
        var validValue = randomValue - remainder;

        return _Encode(validValue);
    }

    private static UInt128 _GetSecureRandomUInt128()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var lower = MemoryMarshal.Read<ulong>(bytes);
        var upper = MemoryMarshal.Read<ulong>(bytes[8..]);

        return new UInt128(lower, upper);
    }
    
    private static LobbyInfo _Encode(UInt128 value)
    {
        var codePayloadBuilder = new StringBuilder(19);
        var currentValue = value;

        for (var i = 0; i < CodeLength; i++)
        {
            if (i is 4 or 8 or 12)
            {
                codePayloadBuilder.Append('-');
            }

            codePayloadBuilder.Append(Chars[(int)(currentValue % 34)]);
            currentValue /= 34;
        }

        var codePayload = codePayloadBuilder.ToString();
        var fullCode = FullCodePrefix + codePayload;

        return new LobbyInfo(LobbyType.Scaffolding, fullCode, $"{NetworkNamePrefix}{codePayload[..9]}", codePayload[10..]);
    }
}