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
    private const int CodeLength = 16;

    private readonly Dictionary<char, ulong> _charToValueMap;

    public LobbyIdParser()
    {
        _charToValueMap = new Dictionary<char, ulong>(36);
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
        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(FullCodePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (code.Length != 21)
        {
            return false;
        }

        UInt128 value = 0;
        UInt128 multiplier = 1;

        var payloadSpan = code.AsSpan(FullCodePrefix.Length);
        var charCount = 0;

        for (var i = 0; i < payloadSpan.Length; i ++)
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

            if (charCount >= CodeLength)
            {
                return false;
            }

            var upperChar = char.ToUpperInvariant(ch);
            if (!_charToValueMap.TryGetValue(upperChar, out var charValue))
            {
                return false;
            }

            value += charValue * multiplier;
            multiplier *= 34;
            charCount++;
        }

        if (charCount != CodeLength)
        {
            return false;
        }

        if (value % 7 != 0)
        {
            return false;
        }

        var codePayload = payloadSpan.ToString().ToUpperInvariant();
        lobbyInfo = new LobbyInfo(
            LobbyType.Scaffolding,
            FullCodePrefix + codePayload,
            $"{NetworkNamePrefix}{codePayload[..9]}",
            codePayload[10..]);

        return true;
    }
}