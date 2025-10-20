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

        Span<char> payloadChars = stackalloc char[CodeLength];
        var payloadIndex = 0;

        var payloadSpan = input.AsSpan(FullCodePrefix.Length);
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
            if (!_CharToValueMap.TryGetValue(ch, out var charValue))
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
        roomInfo = new LobbyInfo(
            FullCodePrefix + codePayload,
            $"{NetworkNameProfix}{codePayload[..9]}",
            codePayload[10..]);

        return true;
    }

    private static LobbyInfo _Encode(UInt128 value)
    {
        var codeBuilder = new StringBuilder(21);
        var nameBuilder = new StringBuilder(28);
        var secretBuilder = new StringBuilder(9);

        codeBuilder.Append(FullCodePrefix);
        nameBuilder.Append(NetworkNameProfix);

        for (var i = 0; i < CodeLength; i++)
        {
            var v = Chars[(int)(value % 34)];
            value /= 34;

            if (i is 4 or 8 or 12)
            {
                codeBuilder.Append('-');
            }

            codeBuilder.Append(v);

            if (i < 8)
            {
                if (i == 4)
                {
                    nameBuilder.Append('-');
                }

                nameBuilder.Append(v);
            }
            else
            {
                if (i == 12)
                {
                    secretBuilder.Append('-');
                }

                secretBuilder.Append(v);
            }
        }

        return new LobbyInfo(codeBuilder.ToString(), nameBuilder.ToString(), secretBuilder.ToString());
    }

    private static UInt128 _GetSecureRandomUInt128()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var lower = MemoryMarshal.Read<ulong>(bytes);
        var upper = MemoryMarshal.Read<ulong>(bytes[8..]);

        return new UInt128(lower, upper);
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