using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.Server.Abstractions;

namespace PCL.Core.Link.Scaffolding.Server.Handlers;

public class GetServerPortHandler : IRequestHandler
{
    /// <inheritdoc />
    public string RequestType { get; } = "c:server_port";

    /// <inheritdoc />
    public Task<(byte Status, ReadOnlyMemory<byte> Body)> HandleAsync(ReadOnlyMemory<byte> requestBody,
        IServerContext context, string sessionId, CancellationToken ct)
    {
        var port = context.MinecraftServerProt;
        var portBytes = BitConverter.GetBytes(port);

        if (port == 0)
        {
            return Task.FromResult<(byte, ReadOnlyMemory<byte>)>((32, ReadOnlyMemory<byte>.Empty));
        }

        return Task.FromResult<(byte, ReadOnlyMemory<byte>)>((0, portBytes));
    }
}