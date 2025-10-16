using System;
using System.IO;
using System.Threading.Tasks;
using PCL.Core.IO;
using PCL.Core.Link.Interop.ControlLayer;
using PCL.Core.Link.Scaffolding.EasyTier;

namespace PCL.Core.Link.Scaffolding;

public class Controller : ILinkController
{
    private EtLinkNetwork? _easytierInstance;

    public async ValueTask<string> StartByCreate()
    {
        if (_easytierInstance != null)
        {
            await _easytierInstance.Shutdown();
            _easytierInstance = null;
        }

        _easytierInstance = new EtLinkNetwork(Path.Combine(FileService.LocalDataPath, "easytier"));
    }

    public async ValueTask StartByJoin(string joinCode)
    {
        throw new NotImplementedException();
    }

    public async ValueTask Stop()
    {
        if (_easytierInstance == null) return;
        await _easytierInstance.Shutdown();
    }

    public event Action<string>? OnPeerConnected;
    public event Action<string>? OnPeerDisconnected;
}