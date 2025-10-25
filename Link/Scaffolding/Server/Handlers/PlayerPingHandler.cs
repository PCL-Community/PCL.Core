using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.Server.Abstractions;

namespace PCL.Core.Link.Scaffolding.Server.Handlers;

public class PlayerPingHandler : IRequestHandler
{
    /// <inheritdoc />
    public string RequestType { get; } = "c:player_ping";

    private static readonly JsonSerializerOptions _JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc />
    public Task<(byte Status, ReadOnlyMemory<byte> Body)> HandleAsync(ReadOnlyMemory<byte> requestBody,
        IServerContext context, string sessionId, CancellationToken ct)
    {
        try
        {
            var profile = JsonSerializer.Deserialize<PlayerProfile>(requestBody.Span, _JsonOptions);
            if (profile is null || string.IsNullOrEmpty(profile.MachineId))
            {
                return Task.FromResult(((byte)32, ReadOnlyMemory<byte>.Empty));
            }

            context.PlayerProfiles[sessionId] = profile with { Kind = PlayerKind.GUEST };

            return Task.FromResult(((byte)0, ReadOnlyMemory<byte>.Empty));
        }
        catch (JsonException)
        {
            return Task.FromResult(((byte)32, ReadOnlyMemory<byte>.Empty));
        }
    }
}