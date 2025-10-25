using PCL.Core.App;
using PCL.Core.Link.Natayark;
using PCL.Core.Net;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Link.Lobby;

public static class LobbyInfoProvider
{
    public static bool IsLobbyAvailable { get; set; } = false;
    public static bool AllowCustomName { get; set; } = false;
    public static bool RequiresLogin { get; set; } = true;
    public static bool RequiresRealName { get; set; } = true;
    public static int ProtocolVersion { get; set; } = 5;

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
        Terracotta
    }

    /// <summary>
    /// 目标大厅
    /// </summary>
    public static LobbyInfo? TargetLobby { get; set; }
    public static int JoinerLocalPort { get; set; }

    /// <summary>
    /// 获取用于联机显示的用户名
    /// </summary>
    public static string? GetUsername() => AllowCustomName
        ? Config.Link.Username.ReplaceNullOrEmpty(NatayarkProfileManager.NaidProfile.Username)
        : NatayarkProfileManager.NaidProfile.Username;
}
