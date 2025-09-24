using System;
using System.Linq;
using System.Numerics;
using PCL.Core.App;
using PCL.Core.Link.Natayark;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using static PCL.Core.Link.Scaffolding.SCFController;

namespace PCL.Core.Link.Lobby;

public static class LobbyInfoProvider
{
    public static bool IsLobbyAvailable { get; set; } = false;
    public static bool AllowCustomName { get; set; } = false;
    public static bool RequiresLogin { get; set; } = true;
    public static bool RequiresRealName { get; set; } = true;
    public static int ProtocolVersion { get; set; } = 4;

    public static Broadcast? McBroadcast { get; internal set; }
    public static TcpForward? McForward { get; internal set; }

    public class LobbyInfo
    {
        public required string OriginalCode { get; init; }
        public required LobbyType Type { get; init; }
        public required string NetworkName { get; init; }
        public required string NetworkSecret { get; init; }
        /// <summary>
        /// 远程 IP 地址，需要先解析大厅类型再填充
        /// </summary>
        public string? Ip { get; init; }
        /// <summary>
        /// 目标游戏端口
        /// </summary>
        public required int Port { get; init; }
    }

    public enum LobbyType
    {
        // ReSharper disable once InconsistentNaming
        PCLCE,
        Terracotta,
        Scaffolding
    }

    /// <summary>
    /// 目标大厅
    /// </summary>
    public static LobbyInfo? TargetLobby { get; set; }
    public static int JoinerLocalPort { get; set; }

    /// <summary>
    /// 解析大厅编号，并返回 LobbyInfo 对象。若解析失败则返回 null。
    /// </summary>
    public static LobbyInfo? ParseCode(string code)
    {
        code = code.Trim().ToUpper();
        if (string.IsNullOrWhiteSpace(code) || code.Length < 9 || !code.IsASCII())
        {
            LogWrapper.Error("Link", "无效的大厅编号: " + code);
            return null;
        }
        
        // Scaffolding 大厅 (以 U/ 开头, 4段)
        if (code.StartsWith("U/"))
        {
            try
            {
                var infoList = code[2..].Split('-');
                if (infoList.Length != 4 || infoList.Any(s => s.Length != 4))
                {
                    LogWrapper.Error("Link", "无效的 Scaffolding 大厅编号格式: " + code);
                    return null;
                }
                
                // 验证字符集和校验和
                const string validChars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // 排除I,O
                var fullCode = string.Join("", infoList);
                
                // 检查字符有效性
                foreach (var c in fullCode.Where(c => !validChars.Contains(c)))
                {
                    LogWrapper.Error("Link", "大厅编号包含无效字符: " + c);
                    return null;
                }

                // 计算校验和 (所有字符映射值按小端序组合成整数后，必须能被7整除)
                long checksum = 0;
                for (int i = 0; i < fullCode.Length; i++)
                {
                    int charValue = validChars.IndexOf(fullCode[i]);
                    checksum += charValue * (long)Math.Pow(34, i);
                }
                
                if (checksum % 7 != 0)
                {
                    LogWrapper.Error("Link", "大厅编号校验和验证失败: " + code);
                    return null;
                }

                // 提取网络名和密钥
                // 格式：U/NNNN-NNNN-SSSS-SSSS
                // 网络名：scaffolding-mc-NNNN-NNNN
                // 网络密钥：SSSS-SSSS
                var networkName = $"scaffolding-mc-{infoList[0]}-{infoList[1]}";
                var networkSecret = $"{infoList[2]}-{infoList[3]}";
                
                TargetSCFLobby = new SCFLobbyInfo // TODO: 解决ET轮询获取主机信息的问题
                { 
                    Port = 0,
                    Ip = null
                };

                return new LobbyInfo
                {
                    OriginalCode = code,
                    NetworkName = networkName,
                    NetworkSecret = networkSecret,
                    Port = 0, // 游戏端口需要通过SCF协议获取
                    Type = LobbyType.Scaffolding,
                    Ip = null, // IP地址需要通过SCF协议获取
                };
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Link", "大厅编号解析失败，可能是无效的 Scaffolding 大厅编号: " + code);
                return null;
            }
        }
        
        // 陶瓦大厅 (5段)
        if (code.Split('-').Length == 5)
        {
            var matches = code.RegexSearch(RegexPatterns.TerracottaId);
            if (matches.Count == 0)
            {
                LogWrapper.Error("Link", "大厅编号解析失败，可能是无效的陶瓦大厅编号: " + code);
                return null;
            }

            foreach (var match in matches)
            {
                var codeString = match.Replace("I", "1").Replace("O", "0").Replace("-", "");
                BigInteger value = 0;
                var checking = 0;
                const string baseChars = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";
                for (var i = 0; i <= 23; i++) 
                { 
                    var j = baseChars.IndexOf(codeString[i]);
                    value += BigInteger.Parse(j.ToString()) * BigInteger.Pow(34, i);
                    checking = (j + checking) % 34;
                }

                if (checking != baseChars.IndexOf(codeString[24])) { return null; }
                var port = (int)(value % 65536);
                if (port < 100) { return null; }
                return new LobbyInfo
                {
                    OriginalCode = code,
                    NetworkName = codeString.Substring(0, 15).ToLower(),
                    NetworkSecret = codeString.Substring(15, 10).ToLower(),
                    Port = port,
                    Type = LobbyType.Terracotta,
                    Ip = "10.144.144.1"
                };
            }
        }
        
        // PCL CE 大厅 (10位 Base32)
        if (code.Length == 10)
        {
            try
            {
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
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "Link", "大厅编号解析失败，可能是无效的 PCL CE 大厅编号: " + code);
                return null;
            }
        }
        
        LogWrapper.Error("Link", "未知的大厅编号: " + code);
        return null;
    }

    /// <summary>
    /// 获取用于联机显示的用户名
    /// </summary>
    public static string? GetUsername() => AllowCustomName
        ? Config.Link.Username.ReplaceNullOrEmpty(NatayarkProfileManager.NaidProfile.Username)
        : NatayarkProfileManager.NaidProfile.Username;
}