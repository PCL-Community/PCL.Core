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

        Span<char> payloadChars = stackalloc char[CodeLength];
        var payloadIndex = 0;

        var payloadSpan = code.AsSpan(FullCodePrefix.Length);
        for (var i = 0; i < payloadSpan.Length; i++)
        {
            var ch = payloadSpan[i];
            if (ch == '-')
            {
                if (i is not (4 or 9 or 14))
                {
                    return false;
                }

                continue;
            }

            if (payloadIndex >= CodeLength)
            {
                return false;
            }

            payloadChars[payloadIndex++] = char.ToUpperInvariant(ch);
        }

        if (payloadIndex != CodeLength)
        {
            return false;
        }

        UInt128 value = 0;
        for (var i = CodeLength - 1; i >= 0; i--)
        {
            var ch = payloadChars[i];
            if (!_charToValueMap.TryGetValue(ch, out var charValue))
            {
                return false;
            }

            value += charValue * _Power(34, i);
        }

        if (value % 7 != 0)
        {
            return false;
        }

        var codePayload = payloadSpan.ToString().ToUpperInvariant();
        lobbyInfo = new LobbyInfo(
            FullCodePrefix + codePayload,
            $"{NetworkNamePrefix}{codePayload[..9]}",
            codePayload[10..]);

        return true;
    }

    private static UInt128 _Power(ulong b, int exp)
    {
        UInt128 res = 1;
        UInt128 basis = b;

        while (exp > 0)
        {
            if (exp % 2 == 1)
            {
                res *= basis;
            }

            basis *= basis;
            exp /= 2;
        }

        return res;
    }
}