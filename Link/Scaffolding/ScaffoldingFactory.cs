using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.EasyTier;
using PCL.Core.Link.Scaffolding.Models;
using PCL.Core.Link.Scaffolding.Server;
using PCL.Core.Net;

namespace PCL.Core.Link.Scaffolding;

public static class ScaffoldingFactory
{
    public static async Task<ScaffoldingClient> CreateClientAsync(string playerName, string roomCode, RoomType from)
    {
        var machineId = Utils.Secret.Identify.LaunchId;

        if (!RoomCodeGenerator.TryParse(roomCode, out var info))
        {
            throw new ArgumentException("Invalid room share code.", nameof(roomCode));
        }

        var entity = _CreateEasyTierEntity(info, 0, 0, false);
        entity.Launch();
        var players = await entity.GetPlayersAsync().ConfigureAwait(false);
        var hostInfo = players.Players?.FirstOrDefault();

        if (hostInfo is null)
        {
            throw new ArgumentNullException(nameof(hostInfo), "Can not get the host information.");
        }

        var host = from switch
        {
            RoomType.PCLCE => "10.114.51.41",
            RoomType.Terracotta => "10.144.144.1",
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, "Unsupported room type.")
        };

        if (!int.TryParse(hostInfo.HostName[22..], out var scfPort))
        {
            throw new ArgumentException("Invalid hostname.", nameof(hostInfo));
        }

        return new ScaffoldingClient(host, scfPort, playerName, machineId, "pcl2-ce");
    }

    public static ScaffoldingServer CreateServer(int mcPort, string playerName)
    {
        var context = ScaffoldingServerContext.Create(playerName, mcPort);
        var scfPort = NetworkHelper.NewTcpPort();

        var entity = _CreateEasyTierEntity(context.UserRoomInfo, mcPort, scfPort, true);
        entity.Launch();

        var server = new ScaffoldingServer(scfPort, context);

        return server;
    }

    private static EasyTierEntity _CreateEasyTierEntity(RoomInfo room, int mcPort, int port, bool asHost) =>
        new(room, mcPort, port, asHost);
}