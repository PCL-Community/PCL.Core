using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Link.EasyTier
{
    public static class EasyTierRelay
    {
        public class ETRelay
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
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
