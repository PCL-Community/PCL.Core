using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.Models;
using PCL.Core.Link.Scaffolding.Server.Abstractions;
using PCL.Core.Logging;

namespace PCL.Core.Link.Scaffolding.Server;

/// <summary>
/// A server for the Scaffolding data exchange protocol.
/// </summary>
public sealed class ScaffoldingServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly IServerContext _context;
    private readonly Dictionary<string, IRequestHandler> _handlers;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenTask;

    public ImmutableDictionary<string, PlayerProfile> CurrentPlayers => _context.PlayerProfiles.ToImmutableDictionary();

    #region Events

    public event Action? ServerStarted;
    public event Action? ServerStopped;
    public event Action? ServerException;

    #endregion

    public ScaffoldingServer(int port, IServerContext context)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _context = context;

        _handlers = typeof(IRequestHandler).Assembly.GetTypes()
            .Where(ty => ty is { IsClass: true, IsAbstract: false } && typeof(IRequestHandler).IsAssignableFrom(ty))
            .Select(ty => (IRequestHandler)Activator.CreateInstance(ty)!)
            .ToDictionary(key => key.RequestType);
    }

    public void Start()
    {
        _listener.Start();
        _listenTask = _ListenForClientsAsync(_cts.Token);

        ServerStarted?.Invoke();
    }

    private async Task _ListenForClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);

                _ = _HandleClientAsync(tcpClient, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer", "Occurred an exception when server running.");

                // TODO: choose a better way to handle exception.
                break;
            }
        }
    }

    private async Task _HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid().ToString();

        using (tcpClient)
        {
            var stream = tcpClient.GetStream();
            var reader = PipeReader.Create(stream);
            var writer = PipeWriter.Create(stream);

            var readResult = await reader.ReadAsync(ct).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            try
            {
                Span<byte> headerTypeLength = stackalloc byte[1];
                buffer.Slice(0, 1).CopyTo(headerTypeLength);
                var typeLength = headerTypeLength[0];
                var typeInfoBytes = buffer.Slice(1, typeLength);
                var typeInfo = Encoding.UTF8.GetString(typeInfoBytes.ToArray());

                Span<byte> bodyLengthBuffer = stackalloc byte[4];
                buffer.Slice(1 + typeLength, 4).CopyTo(bodyLengthBuffer);
                var bodyLength = BinaryPrimitives.ReadUInt32BigEndian(bodyLengthBuffer);
                var bodyBuffer = buffer.Slice(5 + typeLength, bodyLength);

                if (_handlers.TryGetValue(typeInfo, out var handler))
                {
                    var (status, responseBody) =
                        await handler.HandleAsync(bodyBuffer.ToArray(), _context, sessionId, ct).ConfigureAwait(false);

                    Span<byte> responseHeader = stackalloc byte[5];
                    responseHeader[0] = status;

                    BinaryPrimitives.WriteUInt32BigEndian(responseHeader[1..], (uint)responseBody.Length);

                    await writer.WriteAsync(responseHeader.ToArray(), ct).ConfigureAwait(false);
                    if (responseBody.Length > 0)
                    {
                        await writer.WriteAsync(responseBody, ct).ConfigureAwait(false);
                    }

                    await writer.FlushAsync(ct).ConfigureAwait(false);
                }
                else
                {
                    LogWrapper.Warn("ScaffoldingServer", $"No handler found for request type: {typeInfo}");
                }
            }
            finally
            {
                _context.PlayerProfiles.TryRemove(sessionId, out _);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_listener).ConfigureAwait(false);
        await CastAndDispose(_cts).ConfigureAwait(false);
        if (_listenTask != null) await CastAndDispose(_listenTask).ConfigureAwait(false);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                resource.Dispose();
        }
    }
}