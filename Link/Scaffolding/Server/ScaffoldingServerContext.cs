using System.Collections.Concurrent;
using System.Collections.Generic;
using PCL.Core.Link.Scaffolding.Models;
using PCL.Core.Link.Scaffolding.Server.Abstractions;

namespace PCL.Core.Link.Scaffolding.Server;

public class ScaffoldingServerContext : IServerContext
{
    /// <inheritdoc />
    public ConcurrentDictionary<string, PlayerProfile> PlayerProfiles { get; }

    /// <inheritdoc />
    public ushort MinecraftServerProt { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedProtocols { get; }


    /// <inheritdoc />
    public RoomInfo UserRoomInfo { get; }

    /// <inheritdoc />
    public string PlayerName { get; }

    private ScaffoldingServerContext(
        ConcurrentDictionary<string, PlayerProfile> profiles,
        ushort mcPort,
        IReadOnlyList<string> supportedProtocols,
        RoomInfo info,
        string playerName)
    {
        PlayerProfiles = profiles;
        MinecraftServerProt = mcPort;
        SupportedProtocols = supportedProtocols;
        UserRoomInfo = info;
        PlayerName = playerName;
    }

    public static ScaffoldingServerContext Create(string playerName)
    {
        // TODO: impl this
        ushort port = 25565;

        var profile = new PlayerProfile
        {
            Name = playerName,
            MachineId = Utils.Secret.Identify.LaunchId,
            Vendor = "pcl2-ce",
            Kind = PlayerKind.HOST
        };

        IReadOnlyList<string> supportedProtocols =
            ["c:ping", "c:protocols", "c:server_port", "c:player_ping", "c:player_profile_list"];

        var roomCode = RoomCodeGenerator.Generate();

        var dic = new ConcurrentDictionary<string, PlayerProfile>();
        _ = dic.TryAdd(string.Empty, profile);

        return new ScaffoldingServerContext(dic, port, supportedProtocols, roomCode, playerName);
    }
}