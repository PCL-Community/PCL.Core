using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PCL.Core.Link.Interop.NetworkLayer;

namespace PCL.Core.Link.Scaffolding.EasyTier;

public class EtNetwork(string workDirectory) : INetworkSession
{
    private List<EtPeer> _peers = [];

    private async ValueTask<bool> _checkEasyTierAsync()
    {
        var coreApp = Path.Combine(workDirectory, "easytier-core.exe");
        var cliApp = Path.Combine(workDirectory, "easytier-cli.exe");
        return File.Exists(coreApp) && File.Exists(cliApp);
    }

    public async ValueTask<string> CreateSession(IPeer creatorPeer)
    {
        if (await _checkEasyTierAsync()) throw new FileNotFoundException("EasyTier app is not installed");
    }

    public async ValueTask<bool> JoinSession(string sessionId, string sessionSecret)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<IPeer> GetPeers()
    {
        return _peers;
    }

    public async ValueTask Shutdown()
    {
        throw new System.NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        await Shutdown();
    }
}