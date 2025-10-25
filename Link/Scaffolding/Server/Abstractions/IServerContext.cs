using System.Collections.Concurrent;
using PCL.Core.Link.Scaffolding.Client.Models;

namespace PCL.Core.Link.Scaffolding.Server.Abstractions;

public interface IServerContext
{
    /// <summary>
    /// Gets the list of currently connected player profiles, keyed by a unique session identifier.
    /// </summary>
    ConcurrentDictionary<string, PlayerProfile> PlayerProfiles { get; }

    /// <summary>
    /// Gets the prot of the running Minecraft server.
    /// </summary>
    int MinecraftServerProt { get; }


    /// <summary>
    /// Gets the room information.
    /// </summary>
    LobbyInfo UserLobbyInfo { get; }

    /// <summary>
    /// Player name(Host).
    /// </summary>
    string PlayerName { get; }
}