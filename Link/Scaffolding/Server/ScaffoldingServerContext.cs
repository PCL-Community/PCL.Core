using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.EasyTier;
using PCL.Core.Link.Scaffolding.Server.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PCL.Core.Link.Scaffolding.Server;

public class ScaffoldingServerContext : IServerContext
{
    private ConcurrentDictionary<string, PlayerProfile> _playerProfiles = [];

    /// <inheritdoc />
    public ConcurrentDictionary<string, PlayerProfile> PlayerProfiles
    {
        get => _playerProfiles;
        set
        {
            _playerProfiles = value;
            IReadOnlyList<PlayerProfile> arg = [.. value.Values];
            _ = Task.Run(() => PlayerProfilesChanged?.Invoke(arg));
        }
    }

    /// <inheritdoc />
    public event Action<IReadOnlyList<PlayerProfile>>? PlayerProfilesChanged;

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
            // TODO: Please update ScaffoldingFactory.cs at the same time.
            Vendor = $"PCL CE, EasyTier {EasyTierMetadata.CurrentEasyTierVer}",
            Kind = PlayerKind.HOST
        };

        var roomCode = LobbyCodeGenerator.Generate();

        var dic = new ConcurrentDictionary<string, PlayerProfile>();
        _ = dic.TryAdd(string.Empty, profile);

        return new ScaffoldingServerContext(dic, mcPort, roomCode, playerName);
    }
}