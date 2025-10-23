using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Link.Scaffolding.Abstractions;
using PCL.Core.Link.Scaffolding.Framing;
using PCL.Core.Link.Scaffolding.Models;
using PCL.Core.Link.Scaffolding.Requests;
using PCL.Core.Logging;

namespace PCL.Core.Link.Scaffolding;

/// <summary>
/// A client for the scaffolding data exchange protocol.
/// </summary>
public sealed class ScaffoldingClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _scfPort;
    private readonly SemaphoreSlim _srLock = new(1, 1);
    private TcpClient? _tcpClient;
    private PipeReader? _pipeReader;
    private PipeWriter? _pipeWriter;

    // Heart Beat
    private Task? _heartbeatTask;
    private readonly PlayerPingRequest _playerPingRequest;
    private CancellationTokenSource? _heartbeatCts;

    #region Events

    public event Action<IReadOnlyList<PlayerProfile>>? PlayerListUpdated;
    public event Action? ServerShuttedDown;

    #endregion

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public ScaffoldingClient(string host, int scfPort, string playerName, string machineId, string vendor)
    {
        _host = host;
        _scfPort = scfPort;

        _playerPingRequest = new PlayerPingRequest(playerName, machineId, vendor);
    }

    /// <summary>
    /// Connects to ht eSacffolding server.
    /// </summary>
    public async Task ConnectedAsync(CancellationToken ct = default)
    {
        if (IsConnected)
        {
            return;
        }

        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(_host, _scfPort, ct).ConfigureAwait(false);
            var stream = _tcpClient.GetStream();
            _pipeReader = PipeReader.Create(stream);
            _pipeWriter = PipeWriter.Create(stream);
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "ScaffoldingClient", "Failed to connect to server.");
            ServerShuttedDown?.Invoke();
        }

        _StartHeartbeats();
    }

    private void _StartHeartbeats()
    {
        _heartbeatCts = new CancellationTokenSource();
        _heartbeatTask = _HeartbeatLoopAsync(_heartbeatCts.Token);
    }

    private async Task _HeartbeatLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);

                await SendRequestAsync(_playerPingRequest, ct).ConfigureAwait(false);

                var playerList = await SendRequestAsync(new GetPlayerProfileListRequest(), ct).ConfigureAwait(false);

                PlayerListUpdated?.Invoke(playerList);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWrapper.Error(ex, "[ScaffoldingClient]",
                    "Failed when sending heartbeat message. Maybe server ws shutted down.");

                ServerShuttedDown?.Invoke();
                break;
            }
        }
    }

    public async Task<TResponse> SendRequestAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default)
    {
        if (!IsConnected || _pipeWriter is null || _pipeReader is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        await _srLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await ProtocolWriter.WriteRequestAsync(_pipeWriter, request, ct).ConfigureAwait(false);
            var response = await ProtocolReader.ReadResponseAsync(_pipeReader, ct).ConfigureAwait(false);

            return request.ParseResponseBody(response.Body);
        }
        finally
        {
            _srLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(_srLock).ConfigureAwait(false);
        if (_tcpClient != null) await CastAndDispose(_tcpClient).ConfigureAwait(false);
        if (_heartbeatTask != null) await CastAndDispose(_heartbeatTask).ConfigureAwait(false);
        if (_heartbeatCts != null) await CastAndDispose(_heartbeatCts).ConfigureAwait(false);

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