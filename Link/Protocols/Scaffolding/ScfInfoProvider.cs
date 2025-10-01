using System;
using System.Net;

namespace PCL.Core.Link.Protocols.Scaffolding;

public static class ScfInfoProvider
{
    public class ScfPlayerInfo
    {
        public required IPEndPoint Endpoint { get; init; }
        public string Name { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
    }
}