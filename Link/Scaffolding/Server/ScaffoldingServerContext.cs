using System.Collections.Concurrent;
using PCL.Core.Link.Lobby;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.Server.Abstractions;

namespace PCL.Core.Link.Scaffolding.Server;

public class ScaffoldingServerContext : IServerContext
{
    /// <inheritdoc />
    public ConcurrentDictionary<string, PlayerProfile> PlayerProfiles { get; }

    /// <inheritdoc />
    public int MinecraftServerProt { get; }

    /// <inheritdoc />
    public LobbyInfo UserLobbyInfo { get; }

    /// <inheritdoc />
    public string PlayerName { get; }

    private ScaffoldingServerContext(
        ConcurrentDictionary<string, PlayerProfile> profiles,
        int mcPort,
        LobbyInfo info,
        string playerName)
    {
        PlayerProfiles = profiles;
        MinecraftServerProt = mcPort;
        UserLobbyInfo = info;
        PlayerName = playerName;
    }

    public static ScaffoldingServerContext Create(string playerName, int mcPort)
    {
        var profile = new PlayerProfile
        {
            Name = playerName,
            MachineId = Utils.Secret.Identify.LaunchId,
            Vendor = "pcl2-ce",
            Kind = PlayerKind.HOST
        };

        var roomCode = LobbyInfoGenerator.Generate();

        var dic = new ConcurrentDictionary<string, PlayerProfile>();
        _ = dic.TryAdd(string.Empty, profile);

        return new ScaffoldingServerContext(dic, mcPort, roomCode, playerName);
    }
}