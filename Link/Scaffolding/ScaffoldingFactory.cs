using System;
using PCL.Core.Link.Scaffolding.Server;

namespace PCL.Core.Link.Scaffolding;

public static class ScaffoldingFactory
{
    public static ScaffoldingClient CreateClient(
        string host,
        int port,
        string playerName)
    {
        var machineId = Utils.Secret.Identify.LaunchId;
        return new ScaffoldingClient(host, port, playerName, machineId, "pcl2-ce");
    }

    public static ScaffoldingServer CreateServer(
        string host,
        int port,
        string playerName,
        string machineId,
        string vendor)
    {
        throw new NotImplementedException();
    }
}