using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.Server.Abstractions;
using PCL.Core.Link.Scaffolding.Server.Handlers;
using PCL.Core.Logging;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
    private Task? _cleanupTask;

    private static readonly TimeSpan _PlayerTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _CleanupInterval = TimeSpan.FromSeconds(5);

    #region Events

    public event Action<IReadOnlyList<PlayerProfile>>? ServerStarted;
    public event Action? ServerStopped;
    public event Action<Exception?>? ServerException;
    public event Action<IReadOnlyList<PlayerProfile>>? PlayerProfileChanged;

    private void _OnContextPlayersChanged(IReadOnlyList<PlayerProfile> players)
    {
        PlayerProfileChanged?.Invoke(players);
    }

    #endregion

    public ScaffoldingServer(int port, IServerContext context)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
        _context = context;

        _context.PlayerProfilesChanged += _OnContextPlayersChanged;

        _handlers = new()
        {
            ["c:player_ping"] = new PlayerPingHandler(),
            ["c:server_port"] = new GetServerPortHandler(),
            ["c:player_profiles_list"] = new GetPlayerProfileListHandler(),
            ["c:protocols"] = new GetProtocolsHandler(),
            ["c:ping"] = new PingHandler()
        };
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            LogWrapper.Info("ScaffoldingServer",
                $"Successfully bound to {_listener.LocalEndpoint}. Starting to accept clients.");
        }
        catch (SocketException ex)
        {
            LogWrapper.Error(ex, "ScaffoldingServer",
                $"Failed to start TCP listener on port {((IPEndPoint)_listener.LocalEndpoint).Port}. The port might be in use or blocked.");
            ServerException?.Invoke(ex);
            return; // 启动失败，直接返回
        }

        _listenTask = _ListenForClientsAsync(_cts.Token);
        _cleanupTask = _MonitorPlayerLivenessAsync(_cts.Token);

        _listenTask.ContinueWith(t =>
        {
            LogWrapper.Error(t.Exception, "ScaffoldingServer",
                "The main listening task failed unexpectedly. The server is no longer accepting new connections.");
            ServerException?.Invoke(t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);

        _cleanupTask.ContinueWith(
            t =>
            {
                LogWrapper.Error(t.Exception, "ScaffoldingServer", "The player cleanup task failed unexpectedly.");
            }, TaskContinuationOptions.OnlyOnFaulted);

        LogWrapper.Debug("ScaffoldingServer", "Successfully scheduled server background tasks.");

        ServerStarted?.Invoke(_context.PlayerProfiles);
    }

    private async Task _MonitorPlayerLivenessAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_CleanupInterval, ct).ConfigureAwait(false);

                var now = DateTime.UtcNow;
                var timedOutPlayerKeys = new List<string>();

                foreach (var (sessionId, trackedPlayer) in _context.TrackedPlayers)
                {
                    if (string.IsNullOrEmpty(sessionId))
                    {
                        continue;
                    }

                    if (now - trackedPlayer.LastSeenUtc > _PlayerTimeout)
                    {
                        timedOutPlayerKeys.Add(sessionId);
                    }
                }

                if (timedOutPlayerKeys.Count > 0)
                {
                    bool listChanged = false;
                    foreach (var key in timedOutPlayerKeys)
                    {
                        if (_context.TrackedPlayers.TryRemove(key, out var removedPlayer))
                        {
                            listChanged = true;
                            LogWrapper.Info("ScaffoldingServer",
                                $"Player '{removedPlayer.Profile.Name}' timed out and was removed.");
                        }
                    }

                    if (listChanged)
                    {
                        _context.OnPlayerProfilesChanged();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer", "An error occurred in the player cleanup task.");
            }
        }
    }

    private async Task _ListenForClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                LogWrapper.Debug("ScaffoldingServer", $"Client connected: {tcpClient.Client.RemoteEndPoint}");
                _ = _HandleClientAsync(tcpClient, ct);
            }
            catch (OperationCanceledException)
            {
                LogWrapper.Debug("ScaffoldingServer", "Listening task cancelled.");
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer", "Occurred an exception when server running.");

                try
                {
                    _listener.Stop();
                }
                catch (Exception lisEx)
                {
                    LogWrapper.Error(lisEx, "ScaffoldingServer", "Occurred an exception when stop listening port.");
                }


                ServerStopped?.Invoke();
                break;
            }

            LogWrapper.Debug("ScaffoldingServer", "Listening task finished.");
        }
    }

    private async Task _HandleClientAsync(TcpClient tcpClient, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid().ToString();
        LogWrapper.Debug("ScaffoldingServer", $"New client connected. Session ID: {sessionId}");

        using (tcpClient)
        {
            var stream = tcpClient.GetStream();
            var reader = PipeReader.Create(stream);
            var writer = PipeWriter.Create(stream);

            try
            {
                while (true)
                {
                    var readResult = await reader.ReadAsync(ct).ConfigureAwait(false);
                    var buffer = readResult.Buffer;

                    // 在一个循环中处理缓冲区中所有可能存在的完整消息
                    while (_TryParseFrame(ref buffer, out var requestFrame))
                    {
                        LogWrapper.Debug("ScaffoldingServer",
                            $"Received complete frame. Type: {requestFrame.TypeInfo}, Body Length: {requestFrame.Body.Length}");

                        if (_handlers.TryGetValue(requestFrame.TypeInfo, out var handler))
                        {
                            var (status, responseBody) =
                                await handler.HandleAsync(requestFrame.Body, _context, sessionId, ct)
                                    .ConfigureAwait(false);

                            var responseHeader = new byte[5];
                            responseHeader[0] = status;
                            BinaryPrimitives.WriteUInt32BigEndian(responseHeader.AsSpan(1), (uint)responseBody.Length);

                            await writer.WriteAsync(responseHeader, ct).ConfigureAwait(false);
                            if (responseBody.Length > 0)
                            {
                                await writer.WriteAsync(responseBody, ct).ConfigureAwait(false);
                            }

                            // 在发送每个响应后都刷新，确保数据及时送出
                            var flushResult = await writer.FlushAsync(ct).ConfigureAwait(false);

                            LogWrapper.Debug("ScaffoldingServer",
                                $"Response sent for request type: {requestFrame.TypeInfo}");

                            // 如果客户端已经关闭，并且我们刷新失败，就没必要继续了
                            if (flushResult.IsCanceled || flushResult.IsCompleted)
                            {
                                goto
                                    ConnectionClosed; // i dont want to use goto, but that's better for logic in there.
                            }
                        }
                        else
                        {
                            LogWrapper.Warn("ScaffoldingServer",
                                $"No handler found for request type: {requestFrame.TypeInfo}");
                        }
                    }

                    // 将缓冲区的已处理部分标记为消耗掉
                    reader.AdvanceTo(buffer.Start, buffer.End);

                    // 检查退出条件：客户端已关闭连接
                    if (readResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                /* Client disconnected or server is shutting down */
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer", $"An exception occurred while handling client {sessionId}.");
            }

        ConnectionClosed: // goto 标签

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }
    }

    private static bool _TryParseFrame(ref ReadOnlySequence<byte> buffer,
        out (string TypeInfo, byte[] Body, SequencePosition End) frame)
    {
        frame = default;
        if (buffer.Length < 1) return false;

        var typeLength = buffer.FirstSpan[0];
        long headerLength = 1 + typeLength + 4;
        if (buffer.Length < headerLength) return false;

        var headerSlice = buffer.Slice(0, headerLength);
        ReadOnlySpan<byte> headerSpan;

        if (headerSlice.IsSingleSegment)
        {
            headerSpan = headerSlice.FirstSpan;
        }
        else
        {
            Span<byte> tempBuffer = new byte[(int)headerLength];
            headerSlice.CopyTo(tempBuffer);
            headerSpan = tempBuffer;
        }

        var typeInfoSlice = headerSpan.Slice(1, typeLength);

        var typeInfo = Encoding.UTF8.GetString(typeInfoSlice);

        var bodyLengthSlice = headerSpan.Slice(1 + typeLength, 4);
        var bodyLength = BinaryPrimitives.ReadUInt32BigEndian(bodyLengthSlice);

        if (buffer.Length < headerLength + bodyLength) return false;

        var bodyBuffer = buffer.Slice(headerLength, bodyLength);
        var body = bodyBuffer.ToArray();

        frame = (typeInfo, body, bodyBuffer.End);

        buffer = buffer.Slice(frame.End);

        return true;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        LogWrapper.Debug("ScaffoldingServer", "Come into DisposeAsync().");
        if (!_cts.IsCancellationRequested)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_listenTask != null)
        {
            try
            {
                await _listenTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer",
                    "An exception occurred while awaiting the listen task during disposal.");
            }
        }

        if (_cleanupTask != null)
        {
            try
            {
                await _cleanupTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "ScaffoldingServer",
                    "An exception occurred while awaiting the cleanup task during disposal.");
            }
        }

        _listener.Stop();

        _cts.Dispose();

        LogWrapper.Debug("ScaffoldingServer", "Server and all background tasks stopped gracefully.");

        ServerStopped?.Invoke();
    }
}