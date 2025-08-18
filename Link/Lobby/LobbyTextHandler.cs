using PCL.Core.Link.EasyTier;

namespace PCL.Core.Link.Lobby;

public static class LobbyTextHandler
{
    public static string GetNatTypeChinese(string type)
    {
        if (type.Contains("Open") || type.Contains("NoP")) return "开放";
        if (type.Contains("FullCone")) return "中等 (完全圆锥)";
        if (type.Contains("PortRestricted")) return "中等 (端口受限圆锥)";
        if (type.Contains("Restricted")) return "中等 (受限圆锥)";
        if (type.Contains("SymmetricEasy")) return "严格 (宽松对称)";
        if (type.Contains("Symmetric")) return "严格 (对称)";
        return "未知";
    }

    public static string GetConnectTypeChinese(ETConnectionType type) => type switch
    {
        ETConnectionType.Local => "本机",
        ETConnectionType.P2P => "P2P",
        ETConnectionType.Relay => "中继",
        _ => "未知"
    };

    public static string GetQualityDesc(int quality) => quality switch
    {
        >= 3 => "优秀",
        >= 2 => "一般",
        _ => "较差"
    };
}
