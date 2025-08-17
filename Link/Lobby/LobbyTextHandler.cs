using PCL.Core.Link.EasyTier;

namespace PCL.Core.Link.Lobby
{
    public static class LobbyTextHandler
    {
        public static string GetNatTypeChinese(string type)
        {
            if (type.Contains("Open") || type.Contains("NoP"))
            {
                return "开放";
            }
            else if (type.Contains("FullCone"))
            {
                return "中等（完全圆锥）";
            }
            else if (type.Contains("PortRestricted"))
            {
                return "中等（端口受限圆锥）";
            }
            else if (type.Contains("Restricted"))
            {
                return "中等（受限圆锥）";
            }
            else if (type.Contains("SymmetricEasy"))
            {
                return "严格（宽松对称）";
            }
            else if (type.Contains("Symmetric"))
            {
                return "严格（对称）";
            }
            else
            {
                return "未知";
            }
        }

        public static string GetConnectTypeChinese(EasyTierInfoProvider.ETConnectionType type)
        {
            switch (type)
            {
                case EasyTierInfoProvider.ETConnectionType.Local:
                    return "本机";
                case EasyTierInfoProvider.ETConnectionType.P2P:
                    return "P2P";
                case EasyTierInfoProvider.ETConnectionType.Relay:
                    return "中继";
                default:
                    return "未知";
            }
        }

        public static string GetQualityDesc(int quality)
        {
            if (quality >= 3)
            {
                return "优秀";
            }
            else if (quality >= 2)
            {
                return "一般";
            }
            else
            {
                return "较差";
            }
        }
    }
}
