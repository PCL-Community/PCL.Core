using System;
using System.Threading.Tasks;
using PCL.Core.Link.Interop.NetworkLayer;

namespace PCL.Core.Link.Interop.ControlLayer;

public interface ILinkController
{
    public ValueTask<string> StartByCreate();
    public ValueTask StartByJoin(string joinCode);
    event Action<string> OnPeerConnected;
    event Action<string> OnPeerDisconnected;

}