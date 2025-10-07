using System;
using System.Numerics;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;

namespace PCL.Core.Link.Lobby.Parser;

/// <summary>
/// 陶瓦联机的房间号解析器，用于验证和解析陶瓦联机生成的房间号
/// </summary>
public class TerracottaParser : IParser
{
    /// <inheritdoc />
    public (bool isValid, string? error) Validate(string code)
    {   
        // 检查格式是否正确，陶瓦房间号应该由5段组成，用连字符分隔
        if (code.Split("-".ToCharArray()).Length != 5)
        {
            return (false, "格式不正确，应为5段由连字符分隔的字符串");
        }
        
        var matches = code.RegexSearch(RegexPatterns.TerracottaId);
        if (matches.Count == 0)
        {
            return (false, "包含无效字符，无法解析为Terracotta LobbyId");
        }

        // 验证每个匹配项
        foreach (var match in matches)
        {
            var codeString = match.Replace("I", "1").Replace("O", "0").Replace("-", "");
            BigInteger value = 0;
            var checking = 0;
            const string baseChars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            
            // 计算校验和并验证校验位
            for (var i = 0; i <= 23; i++)
            {
                var j = baseChars.IndexOf(codeString[i]);
                value += BigInteger.Parse(j.ToString()) * BigInteger.Pow(34, i);
                checking = (j + checking) % 34;
            }

            if (checking != baseChars.IndexOf(codeString[24]))
            {
                return (false, "校验位不匹配，LobbyId可能已损坏");
            }

            var port = (int)(value % 65536);
            if (port < 100)
            {
                return (false, "端口号无效，应大于等于100");
            }

            return (true, null);
        }
        return (false, "未知错误，无法验证LobbyId");
    }

    /// <inheritdoc />
    public LobbyInfo Parse(string code)
    {
        var matches = code.RegexSearch(RegexPatterns.TerracottaId);

        foreach (var match in matches)
        {
            var codeString = match.Replace("I", "1").Replace("O", "0").Replace("-", "");
            BigInteger value = 0;
            const string baseChars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            
            // 解析代码并计算数值
            for (var i = 0; i <= 23; i++)
            {
                var j = baseChars.IndexOf(codeString[i]);
                value += BigInteger.Parse(j.ToString()) * BigInteger.Pow(34, i);
            }
            
            return new LobbyInfo
            {
                OriginalCode = code,
                NetworkName = codeString[..15].ToLower(),
                NetworkSecret = codeString.Substring(15, 10).ToLower(),
                Port = (int)(value % 65536),
                Type = LobbyType.Terracotta,
                Ip = "10.144.144.1"
            };
        }
        
        throw new ArgumentException("无效的Terracotta LobbyId");
    }
}