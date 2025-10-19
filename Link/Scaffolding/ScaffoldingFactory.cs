using System;
using System.Linq;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.Client;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.EasyTier;
using PCL.Core.Link.Scaffolding.Server;
using PCL.Core.Net;

namespace PCL.Core.Link.Scaffolding;

public static class ScaffoldingFactory
{
    // TODO: change pcl-ce version code when update
    private const string LobbyVendor = $"PCL CE Ver 0.0.0, EasyTier {EasyTierMetadata.CurrentEasyTierVer}";

    public static async Task<ScaffoldingClientEntity> CreateClientAsync(string playerName, string lobbyCode,
        LobbyType from)
    {
        var machineId = Utils.Secret.Identify.LaunchId;

        if (!LobbyCodeGenerator.TryParse(lobbyCode, out var info))
        {
            throw new ArgumentException("Invalid lobby share code.", nameof(lobbyCode));
        }

        var etEntity = _CreateEasyTierEntity(info, 0, 0, false);
        etEntity.Launch();
        var players = await etEntity.GetPlayersAsync().ConfigureAwait(false);
        var hostInfo = players.Players?.FirstOrDefault();

        if (hostInfo is null)
        {
            throw new ArgumentNullException(nameof(hostInfo), "Can not get the host information.");
        }

        var host = from switch
        {
            LobbyType.PCLCE => "10.114.51.41",
            LobbyType.Terracotta => "10.144.144.1",
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, "Unsupported room type.")
        };

        if (!int.TryParse(hostInfo.HostName[22..], out var scfPort))
        {
            throw new ArgumentException("Invalid hostname.", nameof(hostInfo));
        }

        return new ScaffoldingClientEntity(new ScaffoldingClient(host, scfPort, playerName, machineId, LobbyVendor),
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