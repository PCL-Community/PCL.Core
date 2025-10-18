using System.Collections.Concurrent;
using System.Collections.Generic;
using PCL.Core.Link.Scaffolding.Models;

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
    ushort MinecraftServerProt { get; }

    /// <summary>
    /// Gets the list of supported protocols by the server.
    /// </summary>
    IReadOnlyList<string> SupportedProtocols { get; }

    /// <summary>
    /// Gets the room information.
    /// </summary>
    RoomInfo UserRoomInfo { get; }

    /// <summary>
    /// Player name(Host).
    /// </summary>
    string PlayerName { get; }
}