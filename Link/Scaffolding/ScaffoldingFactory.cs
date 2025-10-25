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
    // Please update ScaffoldingServerContext.cs at the same time.
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

        if (info.Type != LobbyType.Scaffolding)
        {
            throw new ArgumentException("Invalid lobby type.", nameof(lobbyCode));
        }

        var etEntity = _CreateEasyTierEntity(info, 0, 0, false);
        etEntity.Launch();
        var retries = 0;
        while (etEntity.State != EtState.Ready && retries < 6)
        {
            await etEntity.CheckEasyTierStatusAsync();
            await Task.Delay(800);
            retries++;
        }
        var players = await etEntity.GetPlayersAsync().ConfigureAwait(false);
        EasyPlayerInfo? hostInfo = null;
        foreach (var player in players.Players!)
        {
            if (player.HostName.Contains("scaffolding-mc-server-"))
            {
                hostInfo = player;
            }
        }

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

        var localPort = await etEntity.AddPortForward(hostInfo.Ip, scfPort);
        
        return new ScaffoldingClientEntity(new ScaffoldingClient("127.0.0.1", localPort, playerName, machineId, LobbyVendor),
            etEntity, hostInfo);
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

public record ScaffoldingClientEntity(ScaffoldingClient Client, EasyTierEntity EasyTier, EasyPlayerInfo HostInfo);

public record ScaffoldingServerEntity(ScaffoldingServer Server, EasyTierEntity EasyTier);
