using PCL.Core.App;
using PCL.Core.Link.Natayark;
using PCL.Core.Link.Scaffolding;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Link.Scaffolding.EasyTier;
using PCL.Core.Logging;
using PCL.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PCL.Core.Link.Lobby;

/// <summary>
/// Lobby server. For auto-mangement
/// </summary>
[LifecycleService(LifecycleState.Loaded)]
public class LobbyService() : GeneralService("lobby", "LobbyService")
{
    private static readonly LobbyController _LobbyController = new();
    private static CancellationTokenSource _lobbyCts = new();

    private static readonly Timer _ServerGameWatcher =
        new(_CheckGameState, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));

    private static bool _isGameWatcherRunnable = false;

    /// <summary>
    /// Current lobby state.
    /// </summary>
    public static LobbyState CurrentState { get; private set; } = LobbyState.Idle;


    /// <summary>
    /// Founded local Minecraft worlds.
    /// </summary>
    public static ObservableCollection<FoundWorld> DiscoveredWorlds { get; } = [];

    /// <summary>
    /// Current players in current lobby.
    /// </summary>
    public static ObservableCollection<PlayerProfile> Players { get; private set; } = [];

    /// <summary>
    /// Demonstrate wheather the current user is the host of the lobby.
    /// </summary>
    public static bool IsHost => _LobbyController.IsHost;

    /// <summary>
    /// Current lobby full code.
    /// </summary>
    public static string? CurrentLobbyCode { get; private set; }

    /// <summary>
    /// Current lobby user name.
    /// </summary>
    public static string? CurrentUserName { get; private set; }

    /// <summary>
    /// Procheck result.
    /// </summary>
    public static LobbyCheckResult CheckResult { get; set; } = new(false, string.Empty, CoreHintType.Info);

    #region UI Events

    /// <summary>
    /// Invoked when lobby state changed. (first arg is old state; second arg is new satte.)
    /// </summary>
    public static event Action<LobbyState, LobbyState>? StateChanged;

    /// <summary>
    /// Used for UI layer to send Hint.
    /// </summary>
    public static event Action<string, CoreHintType>? OnHint;

    /// <summary>
    /// Invoked when need to download EasyTier core files.
    /// </summary>
    public static event Action? OnNeedDownloadEasyTier;

    /// <summary>
    /// Invoked when user stop the game in server mode.
    /// </summary>
    public static event Action? OnUserStopGame;

    /// <summary>
    /// Invoked when client ping happened.
    /// </summary>
    public static event Action<long>? OnClientPing;

    #endregion

    /// <inheritdoc />
    public override void Stop()
    {
        _ = _LobbyController.CloseAsync();
        _ServerGameWatcher.Dispose();
        _lobbyCts.Dispose();
    }

    private static bool _IsEasyTierCoreFileNotExist() =>
        !File.Exists(Path.Combine(EasyTierMetadata.EasyTierFilePath, "easyiter-core.exe")) &&
        !File.Exists(Path.Combine(EasyTierMetadata.EasyTierFilePath, "Packet.dll")) &&
        !File.Exists(Path.Combine(EasyTierMetadata.EasyTierFilePath, "easyiter-cli.exe"));


    public static async Task InitializeAsync()
    {
        if (CurrentState is not LobbyState.Idle && CurrentState is not LobbyState.Error)
        {
            return;
        }

        _SetState(LobbyState.Initializing);
        try
        {
            if (_IsEasyTierCoreFileNotExist())
            {
                LogWrapper.Info("LobbyService", "EasyTier not found, starting download.");
                OnNeedDownloadEasyTier?.Invoke();
            }
            else
            {
                LogWrapper.Info("LobbyService", "EasyTier files check completed.");
            }

            // refresh naid token
            var naidRefreshToken = Config.Link.NaidRefreshToken;
            if (!string.IsNullOrWhiteSpace(naidRefreshToken))
            {
                var expTime = Config.Link.NaidRefreshExpireTime;
                if (!string.IsNullOrWhiteSpace(expTime) &&
                    Convert.ToDateTime(expTime).CompareTo(DateTime.Now) < 0)
                {
                    Config.Link.NaidRefreshToken = string.Empty;
                    OnHint?.Invoke("Natayark ID 令牌已过期，请重新登录", CoreHintType.Critical);
                }
                else
                {
                    await NatayarkProfileManager.GetNaidDataAsync(naidRefreshToken, true).ConfigureAwait(false);
                }
            }

            _SetState(LobbyState.Initialized);
            LogWrapper.Info("LobbySerivce", "Lobby service initialized succefully.");
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "LobbyService", "Lobby service initialization failed.");
            OnHint?.Invoke("大厅服务初始化失败，请检查网络连接。", CoreHintType.Critical);
            _SetState(LobbyState.Error);
        }
    }

    /// <summary>
    /// Discover minecraft shared world.
    /// </summary>
    public static async Task DiscoverWorldAsync()
    {
        if (CurrentState is not LobbyState.Initialized && CurrentState is not LobbyState.Idle)
        {
            return;
        }

        _SetState(LobbyState.Discovering);
        DiscoveredWorlds.Clear();

        await Task.Run(async () =>
        {
            var recordedPorts = new ConcurrentSet<int>();
            using var listener = new BroadcastListener();

            var handler = new Action<BroadcastRecord, IPEndPoint>(async (info, _) =>
            {
                if (!recordedPorts.TryAdd(info.Address.Port)) return;

                using var pinger = new McPing(new IPEndPoint(IPAddress.Loopback, info.Address.Port));
                using var cts = new CancellationTokenSource(2000);

                try
                {
                    var pingRes = await pinger.PingAsync(cts.Token).ConfigureAwait(false);

                    if (pingRes is null)
                    {
                        throw new ArgumentNullException(nameof(pingRes), "Failed to ping minecraft entity.");
                    }

                    var worldName = $"{pingRes.Description} / {pingRes.Version.Name} ({info.Address.Port})";
                    await _RunInUiAsync(() => DiscoveredWorlds.Add(new FoundWorld(worldName, info.Address.Port)))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogWrapper.Error(ex, "LobbyService", $"Pinging port {info.Address.Port} failed.");
                }
            });

            listener.OnReceive += handler;
            listener.Start();
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            listener.OnReceive -= handler;
        }).ConfigureAwait(false);

        _SetState(LobbyState.Initialized);
    }

    private static bool _NotHaveNaid() =>
        LobbyInfoProvider.RequiresLogin &&
        string.IsNullOrWhiteSpace(NatayarkProfileManager.NaidProfile.AccessToken);

    /// <summary>
    /// Create a new lobby.
    /// </summary>
    /// <param name="port">Minceaft share port.</param>
    /// <param name="username">Player name.</param>
    public static async Task<bool> CreateLobbyAsync(int port, string username)
    {
        if (!CheckResult.IsAbleToStart)
        {
            return false;
        }

        if (_NotHaveNaid())
        {
            OnHint?.Invoke("请先登录 Natayark ID 再使用大厅！", CoreHintType.Critical);
            return false;
        }

        _SetState(LobbyState.Creating);

        try
        {
            CurrentUserName = username;

            var serverEntity = await _LobbyController.LaunchServerAsync(username, port).ConfigureAwait(false);
            if (serverEntity is null)
            {
                OnHint?.Invoke("在创建房间的时候遇到了问题，请查看日志并将此问题反馈给开发者！", CoreHintType.Critical);
                return false;
            }

            CurrentLobbyCode = serverEntity.EasyTier.Lobby.FullCode;

            _SetState(LobbyState.Connected);
            _isGameWatcherRunnable = true;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "LobbyService", "Fialed to create lobby.");
            OnHint?.Invoke("创建大厅失败，请检查日志或向开发者反馈。", CoreHintType.Critical);
            await LeaveLobbyAsync().ConfigureAwait(false);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Join a exist lobby.
    /// </summary>
    /// <param name="lobbyCode">Lobby share code.</param>
    /// <param name="username">Current use name.</param>
    public static async Task<bool> JoinLobbyAsync(string lobbyCode, string username)
    {
        if (!CheckResult.IsAbleToStart)
        {
            return false;
        }

        _SetState(LobbyState.Joining);

        LogWrapper.Info("LobbyService", $"Try to join lobby {lobbyCode}");

        try
        {
            CurrentUserName = username;
            CurrentLobbyCode = lobbyCode;

            var clientEntity = await _LobbyController.LaunchClientAsync(username, lobbyCode).ConfigureAwait(false);

            if (clientEntity is null)
            {
                throw new InvalidOperationException(
                    "Failed to join lobby. The LobbyCode might be incorrect or the lobby is not exist.");
            }

            clientEntity.Client.Heartbeat += _ClientOnHeartbeat;

            _SetState(LobbyState.Connected);
        }
        catch (ArgumentException codeEx)
        {
            LogWrapper.Error(codeEx, "LobbyService", $"Fialed to join lobby {lobbyCode}.");
            OnHint?.Invoke("房间码不正确，请检查！", CoreHintType.Critical);
            await LeaveLobbyAsync().ConfigureAwait(false);

            return false;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "LobbyService", $"Fialed to join lobby {lobbyCode}.");
            OnHint?.Invoke(ex.Message, CoreHintType.Critical);
            await LeaveLobbyAsync().ConfigureAwait(false);

            return false;
        }

        return true;
    }

    private static void _ClientOnHeartbeat(IReadOnlyList<PlayerProfile> players, long latency)
    {
        var sortedPlayers = PlayerListHandler.Sort(players);
        Players = [.. sortedPlayers];
        OnClientPing?.Invoke(latency);
    }


    /// <summary>
    /// Leave from lobby.
    /// </summary>
    public static async Task LeaveLobbyAsync()
    {
        _SetState(LobbyState.Leaving);

        await _lobbyCts.CancelAsync().ConfigureAwait(false);

        Players.Clear();
        CurrentLobbyCode = null;
        CurrentUserName = null;
        await _LobbyController.CloseAsync().ConfigureAwait(false);

        _lobbyCts = new CancellationTokenSource();
        _SetState(LobbyState.Initialized);

        LogWrapper.Info("LobbyService", "Left lobby and cleaned up resources.");
    }

    private static void _SetState(LobbyState newState)
    {
        var oldState = CurrentState;
        if (oldState == newState)
        {
            return;
        }

        CurrentState = newState;

        LogWrapper.Info("LobbyService", $"Lobby state changed from {oldState} to {newState}");

        StateChanged?.Invoke(oldState, newState);
    }

    private static void _CheckGameState(object? state)
    {
        if (!_isGameWatcherRunnable)
        {
            return;
        }

        LobbyController.IsHostInstanceAvailableAsync(_LobbyController.ScfServerEntity.EasyTier.MinecraftPort)
            .ContinueWith(async (task) =>
            {
                var isExist = await task.ConfigureAwait(false);
                if (!isExist)
                {
                    _isGameWatcherRunnable = false;
                    OnUserStopGame?.Invoke();
                }
            });
    }

    private static async Task _RunInUiAsync(Action action)
    {
        await Application.Current.Dispatcher.InvokeAsync(action);
    }
}

/// <summary>
/// Founded minecraft world information.
/// </summary>
/// <param name="Name">World name.</param>
/// <param name="Port">World share port.</param>
public record FoundWorld(string Name, int Port);

/// <summary>
/// Lobby check result.
/// </summary>
/// <param name="IsAbleToStart">Demonstrate is lobby ready to start.</param>
/// <param name="Message">Precheck result message.</param>
/// <param name="HintType">Hint type.</param>
public record LobbyCheckResult(bool IsAbleToStart, string Message, CoreHintType HintType);

/// <summary>
/// Hint type in PCL.Core (for UI display).
/// </summary>
public enum CoreHintType
{
    /// <summary>
    /// 信息，通常是蓝色的“i”。
    /// </summary>
    /// <remarks></remarks>
    Info,

    /// <summary>
    /// 已完成，通常是绿色的“√”。
    /// </summary>
    /// <remarks></remarks>
    Finish,

    /// <summary>
    /// 错误，通常是红色的“×”。
    /// </summary>
    /// <remarks></remarks>
    Critical
}