using System;
using System.Collections.Generic;
using PCL.Core.Link.Interop.ControlLayer;

namespace PCL.Core.Link.Interop.NetworkLayer;

public interface ILinkPeer
{
    string Name { get; set; }                 // 客户端名称
    string Id { get; }                        // Peer ID
    ConnectionStatus Status { get; set; }     // 以何种方式连入网络
    long Tx { get; set; }                     // 发送字节数
    long Rx { get; set; }                     // 接收字节数
    int Latency { get; set; }                 // 延迟
    int PacketLoss { get; set; }              // 丢包率
    DateTime LastHeartbeat { get; set; }      // 最后心跳时间
    Dictionary<PeerMetadata, object> Metadata { get; } // 扩展字段（如用户ID、角色等）
}