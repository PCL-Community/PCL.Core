using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.Link.Interop.NetworkLayer;

public interface INetworkSession : IAsyncDisposable
{
    public ValueTask<string> CreateSession(IPeer creatorPeer);
    public ValueTask<bool> JoinSession(string sessionId, string sessionSecret);
    public IEnumerable<IPeer> GetPeers();
    public ValueTask Shutdown();
}