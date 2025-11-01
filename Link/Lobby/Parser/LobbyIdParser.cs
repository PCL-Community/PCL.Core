// This code is from terracotta project.
// Thanks for Burning_TNT's contribution!
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Lobby.Parser;

public class LobbyIdParser : ILobbyIdParser
{
    private const string Chars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string FullCodePrefix = "U/";
    private const string NetworkNamePrefix = "scaffolding-mc-";
    private const int BaseVal = 34;

    private const int DataLength = 16; // NNNN NNNN SSSS SSSS (16 chars)
    private const int HyphenCount = 3;

    private readonly Dictionary<char, byte> _charToValueMap;

    public LobbyIdParser()
    {
        _charToValueMap = new Dictionary<char, byte>(36);
        for (byte i = 0; i < Chars.Length; i++)
        {
            _charToValueMap[Chars[i]] = i;
        }

        _charToValueMap['I'] = 1;
        _charToValueMap['O'] = 0;
    }
    
    public bool TryParse(string code, [NotNullWhen(true)] out LobbyInfo? lobbyInfo)
    {
        lobbyInfo = null;

        if (string.IsNullOrWhiteSpace(code) ||
            !code.StartsWith(FullCodePrefix, StringComparison.Ordinal) ||
            code.Length != 21)
        {
            return false;
        }

        Span<byte> values = stackalloc byte[DataLength];
        var valueIndex = 0;
        var payloadSpan = code.AsSpan(FullCodePrefix.Length);

        for (var i = 0; i < payloadSpan.Length; i++)
        {
            var ch = payloadSpan[i];
            if (ch == '-')
            {
                if (i != 4 && i != 9 && i != 14)
                {
                    return false;
                }

                continue;
            }

            if (valueIndex >= DataLength ||
                !_charToValueMap.TryGetValue(char.ToUpperInvariant(ch), out var charValue))
            {
                return false;
            }

            values[valueIndex++] = charValue;
        }

        if (valueIndex != DataLength)
        {
            return false;
        }

        UInt128 value = 0;
        for (var i = DataLength - 1; i >= 0; i--)
        {
            value = value * BaseVal + values[i];
        }

        if (value % 7 != 0)
        {
            return false;
        }

        var networkNamePayload = payloadSpan[..9];
        var networkSecretPayload = payloadSpan[10..];

        lobbyInfo = new LobbyInfo(
            LobbyType.Scaffolding,
            string.Concat(FullCodePrefix, payloadSpan).ToUpperInvariant(),
            string.Concat(NetworkNamePrefix, networkNamePayload),
            networkSecretPayload.ToString());

        return true;
    }
}