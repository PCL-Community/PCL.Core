using System.Collections.Generic;

namespace PCL.Core.Link.EasyTier
{
    public static class EasyTierRelay
    {
        public class ETRelay
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public ETRelayType Type { get; set; }
        }
        public enum ETRelayType
        {
            Community,
            Selfhosted,
            Custom
        }

        public static List<ETRelay> RelayList;
    }
}
