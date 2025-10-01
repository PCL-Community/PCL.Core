using System;
using System.Collections.Generic;
using PCL.Core.Link.Interop.ControlLayer;
using PCL.Core.Link.Interop.NetworkLayer;

namespace PCL.Core.Link.Scaffolding.EasyTier;

public class EtPeer : IPeer
{
    public string Name { get; init; }
    public string Id { get; init; }
    public ConnectionStatus Status { get; set; }
    public long Tx { get; set; }
    public long Rx { get; set; }
    public int Latency { get; set; }
    public int PacketLoss { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public Dictionary<PeerMetadata, object> Metadata { get; init; }
}