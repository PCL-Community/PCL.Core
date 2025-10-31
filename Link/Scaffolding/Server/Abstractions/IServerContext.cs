using PCL.Core.Link.Scaffolding.Client.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PCL.Core.Link.Scaffolding.Server.Abstractions;

public interface IServerContext
{
    /// <summary>
    /// Gets the list of currently connected player profiles, keyed by a unique session identifier.
    /// </summary>
    ConcurrentDictionary<string, PlayerProfile> PlayerProfiles { get; }

    /// <summary>
    /// Occurs when the list of player profiles changes.
    /// </summary>
    event Action<IReadOnlyList<PlayerProfile>> PlayerProfilesChanged;

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