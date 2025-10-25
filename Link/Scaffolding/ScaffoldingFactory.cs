using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.Link.Lobby;
using PCL.Core.Link.Scaffolding.Client;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.EasyTier;
using PCL.Core.Link.Scaffolding.Server;
using PCL.Core.Net;

namespace PCL.Core.Link.Scaffolding;

public static class ScaffoldingFactory
{
    // TODO: change pcl-ce version code when update
    private const string LobbyVendor = $"PCL CE 0.0.0, EasyTier {EasyTierMetadata.CurrentEasyTierVer}";
    private const string HostIp = "10.114.51.41";

    public static async Task<ScaffoldingClientEntity> CreateClientAsync(string playerName, string lobbyCode,
        LobbyType from)
    {
        var machineId = Utils.Secret.Identify.LaunchId;
        var info = LobbyInfoGenerator.Parse(lobbyCode);
        if (info == null)
        {
            throw new ArgumentException("Invalid lobby code.", nameof(lobbyCode));
        }

        var etEntity = _CreateEasyTierEntity(info, 0, 0, false);
        etEntity.Launch();
        var players = await etEntity.GetPlayersAsync().ConfigureAwait(false);
        var hostInfo = players.Players?.FirstOrDefault();

        if (hostInfo is null)
        {
            etEntity.Stop();
            throw new ArgumentNullException(nameof(hostInfo), "Can not get the host information.");
        }

        if (!int.TryParse(hostInfo.HostName[22..], out var scfPort))
        {
            etEntity.Stop();
            throw new ArgumentException("Invalid hostname.", nameof(hostInfo));
        }

        return new ScaffoldingClientEntity(new ScaffoldingClient(HostIp, scfPort, playerName, machineId, LobbyVendor),
            etEntity);
    }

    public static ScaffoldingServerEntity CreateServer(int mcPort, string playerName)
    {
        var context = ScaffoldingServerContext.Create(playerName, mcPort);
        var scfPort = NetworkHelper.NewTcpPort();

        var etEntity = _CreateEasyTierEntity(context.UserLobbyInfo, mcPort, scfPort, true);
        etEntity.Launch();

        var server = new ScaffoldingServer(scfPort, context);

        return new ScaffoldingServerEntity(server, etEntity);
    }

    private static EasyTierEntity _CreateEasyTierEntity(LobbyInfo lobby, int mcPort, int port, bool asHost) =>
        new(lobby, mcPort, port, asHost);
}

public record ScaffoldingClientEntity(ScaffoldingClient Client, EasyTierEntity EasyTier);

public record ScaffoldingServerEntity(ScaffoldingServer Server, EasyTierEntity EasyTier);
