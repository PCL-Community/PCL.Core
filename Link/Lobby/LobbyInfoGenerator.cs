using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    private const int BaseVal = 34;

    private const int DataLength = 16; // NNNN NNNN SSSS SSSS (16 chars)
    private const int HyphenCount = 3;
    private const int PayloadLength = DataLength + HyphenCount; // 19

    private static readonly UInt128 _EncodingMaxValue = _CalculatePower(BaseVal, DataLength);
    
    /// <summary>
    /// 房间号解析器列表
    /// </summary>
    private static readonly IReadOnlyList<ILobbyIdParser> _Parsers = [
        new OldLobbyIdParser(),
        new LobbyIdParser()
    ];

    /// <summary>
    /// 解析指定的房间号并输出 LobbyInfo 对象
    /// </summary>
    /// <param name="code">要解析的房间号</param>
    /// <param name="lobbyInfo">解析成功时输出的 LobbyInfo 对象，解析失败时为 null</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParse(string code, [NotNullWhen(true)] out LobbyInfo? lobbyInfo)
    {
        lobbyInfo = null;
        try
        {
            foreach (var parser in _Parsers)
            {
                if (parser.TryParse(code, out lobbyInfo))
                {
                    return true;
                }
            }
            
            LogWrapper.Warn("Link", $"无法解析房间号, 可能为无效或无法识别的房间号");
            return false;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Link", "解析房间号时发生异常");
            return false;
        }
    }
    
    /// <summary>
    /// 生成一个CE大厅
    /// </summary>
    /// <returns>返回一个<see cref="LobbyInfo"/></returns>
    public static LobbyInfo Generate()
    {
        var randomValue = _GetSecureRandomUInt128();
        var valueInRange = randomValue % _EncodingMaxValue;
        var remainder = valueInRange % 7;
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
        var codePayload = string.Create(PayloadLength, value, (span, val) =>
        {
            Span<char> tempChars = stackalloc char[DataLength];
            for (var i = 0; i < DataLength; i++)
            {
                tempChars[i] = Chars[(int)(val % BaseVal)];
                val /= BaseVal;
            }

            tempChars[..4].CopyTo(span[..4]);
            span[4] = '-';
            tempChars[4..8].CopyTo(span[5..9]);
            span[9] = '-';
            tempChars[8..12].CopyTo(span[10..14]);
            span[14] = '-';
            tempChars[12..16].CopyTo(span[15..]);
        });

        var networkNamePayload = codePayload.AsSpan(0, 9);
        var networkSecretPayload = codePayload.AsSpan(10);

        return new LobbyInfo(
            LobbyType.Scaffolding,
            string.Concat(FullCodePrefix, codePayload),
            string.Concat(NetworkNamePrefix, networkNamePayload),
            networkSecretPayload.ToString());
    }

    private static UInt128 _CalculatePower(uint baseVal, int exp)
    {
        UInt128 result = 1;
        for (var i = 0; i < exp; i++)
        {
            result *= baseVal;
        }

        return result;
    }
}