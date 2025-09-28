using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Net;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Link.Protocols.Scaffolding;

public class ScfProtocol(bool isServer) : LinkProtocol(isServer)
{
    protected override string Identifier => "scaffolding";

    protected override void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e)
    {
        // TODO: 实现数据接收处理逻辑
        _ = sender; // 避免未使用参数警告
        _ = e;      // 避免未使用参数警告
    }
    
    protected override Task ClientPart(CancellationToken token)
    {
        // TODO: 实现客户端部分逻辑
        throw new NotImplementedException();
    }
}