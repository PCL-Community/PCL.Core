using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Link.Lobby
{
    public static class LobbyHandler
    {
        public class LobbyInfo
        {
            public string OriginalCode { get; set; }
            public string NetworkName { get; set; }
            public string NetworkSecret { get; set; }
            public int Port { get; set; }
            public LobbyType Type { get; set; }
        }

        public enum LobbyType
        {
            PCLCE,
            Terracotta
        }

        public static LobbyInfo TargetLobby;
        public static int JoinerLocalPort;
    }
}
