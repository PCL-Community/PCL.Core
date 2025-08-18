using System.Collections.Generic;

namespace PCL.Core.Link.EasyTier;
// ReSharper disable InconsistentNaming

public class ETRelay
{
    public static List<ETRelay> RelayList = [];
    public required string Url { get; set; }
    public required string Name { get; set; }
    public ETRelayType Type { get; set; }
}

public enum ETRelayType
{
    Community,
    Selfhosted,
    Custom
}
