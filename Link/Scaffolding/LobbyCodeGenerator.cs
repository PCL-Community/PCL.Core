// This code is from terracota project.
// Thanks for Burning_TNT's contribution!

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Scaffolding;

public static class LobbyCodeGenerator
{
    private const string Chars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string FullCodePrefix = "U/";
    private const string NetworkNamePrefix = "scaffolding-mc-";
    private const int CodeLength = 16;

    private static readonly Dictionary<char, ulong> _CharToValueMap;

    static LobbyCodeGenerator()
    {
        _CharToValueMap = new Dictionary<char, ulong>(36);
        for (byte i = 0; i < Chars.Length; i++)
        {
            _CharToValueMap[Chars[i]] = i;
        }

        _CharToValueMap['I'] = 1;
        _CharToValueMap['O'] = 0;
    }

    public static LobbyInfo Generate()
    {
        var randomValue = _GetSecureRandomUInt128();
        var remainder = randomValue % 7;
        var validValue = randomValue - remainder;

        return _Encode(validValue);
    }

    public static bool TryParse(string input, [NotNullWhen(true)] out LobbyInfo? roomInfo)
    {
        roomInfo = null;
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith(FullCodePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (input.Length != 21)
        {
            return false;
        }

        UInt128 value = 0;
        UInt128 multiplier = 1;

        var payloadSpan = input.AsSpan(FullCodePrefix.Length);
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
            if (!_CharToValueMap.TryGetValue(upperChar, out var charValue))
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
        roomInfo = new LobbyInfo(
            FullCodePrefix + codePayload,
            $"{NetworkNamePrefix}{codePayload[..9]}",
            codePayload[10..]);

        return true;
    }

    private static LobbyInfo _Encode(UInt128 value)
    {
        var codePayloadBuilder = new StringBuilder(19);
        UInt128 currentValue = value;

        for (var i = 0; i < CodeLength; i++)
        {
            if (i == 4 || i == 8 || i == 12)
            {
                codePayloadBuilder.Append('-');
            }

            codePayloadBuilder.Append(Chars[(int)(currentValue % 34)]);
            currentValue /= 34;
        }

        var codePayload = codePayloadBuilder.ToString();
        var fullCode = FullCodePrefix + codePayload;

        return new LobbyInfo(fullCode, $"{NetworkNamePrefix}{codePayload[..9]}", codePayload[10..]);
    }

    private static UInt128 _GetSecureRandomUInt128()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var lower = MemoryMarshal.Read<ulong>(bytes);
        var upper = MemoryMarshal.Read<ulong>(bytes[8..]);

        return new UInt128(lower, upper);
    }
}