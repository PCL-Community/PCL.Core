using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.Link.Interop.NetworkLayer;

public interface ILinkNetworkSession : IAsyncDisposable
{
    public ValueTask<string> CreateSession(ILinkPeer creatorLinkPeer);
    public ValueTask<bool> JoinSession(string sessionId, string sessionSecret);
    public IEnumerable<ILinkPeer> GetPeers();
    public ValueTask Shutdown();
}