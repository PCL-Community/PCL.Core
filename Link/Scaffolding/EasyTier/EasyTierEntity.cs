using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Link.EasyTier;
using PCL.Core.Link.Scaffolding.Client.Models;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;

namespace PCL.Core.Link.Scaffolding.EasyTier;

public enum EtState
{
    Stopped,
    Active,
    Ready
}

public class EasyTierEntity
{
    private Process? _etProcess;
    private readonly int _rpcPort;
    private readonly LobbyInfo _lobby;
    private readonly int _mcPort;
    private readonly int _scfPort;
    public int ForwardPort { get; private set; }
    public EtState State { get; private set; }

    /// <summary>
    /// Constructor of EasyTierEntity
    /// </summary>
    /// <param name="lobby">The room information.</param>
    /// <param name="mcPort">Minecraft port.</param>
    /// <param name="scfPort">The server port.</param>
    /// <param name="asHost">Indicates whether the entity acts as a host.</param>
    /// <exception cref="InvalidOperationException">Thrown if EasyTier was broken.</exception>
    public EasyTierEntity(LobbyInfo lobby, int mcPort, int scfPort, bool asHost)
    {
        _lobby = lobby;
        _mcPort = mcPort;
        _scfPort = scfPort;
        State = EtState.Stopped;

        var existEntities = Process.GetProcessesByName("easytier-core");
        foreach (var entity in existEntities)
        {
            LogWrapper.Warn("EasyTier", $"Find exist EasyTier Entity, may influence some thing: {entity.Id}");
        }

        LogWrapper.Info("EasyTier", "Executable file path: {");
        if (!(File.Exists($"{EasyTierMetadata.EasyTierFilePath}\\easytier-core.exe") &&
              File.Exists($"{EasyTierMetadata.EasyTierFilePath}\\easytier-cli.exe") &&
              File.Exists($"{EasyTierMetadata.EasyTierFilePath}\\Packet.dll")))
        {
            LogWrapper.Error("EasyTier", "EasyTier was broken.");

            throw new InvalidOperationException("EasyTier was broken.");
        }

        State = EtState.Ready;

        _rpcPort = NetworkHelper.NewTcpPort();
        ForwardPort = NetworkHelper.NewTcpPort();

        _etProcess = _BuildProcess(asHost);
    }

    /// <summary>
    /// Launchs EasyTier process.
    /// </summary>
    /// <returns>
    /// - 1 means failed to launch EasyTire and never can launch again.<br/>
    /// - 0 means successful launch.
    /// </returns>
    public int Launch()
    {
        try
        {
            _etProcess!.Start();
            State = EtState.Active;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "EasyTire", "Failed to launch EasyTire.");
            State = EtState.Stopped;
            _etProcess = null;
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Stops EasyTier process.
    /// </summary>
    /// <returns>
    /// - 1 means failed to stop EasyTier.<br/>
    /// - 0 means successful stop.
    /// </returns>
    public int Stop()
    {
        try
        {
            _etProcess!.Kill();
            State = EtState.Stopped;
            return 0;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "EasyTier", "Failed to stop EasyTier.");
            State = EtState.Stopped;
            _etProcess = null;
            return 1;
        }
    }

    private Process _BuildProcess(bool asHost)
    {
        var process = new Process
        {
            EnableRaisingEvents = true,
            StartInfo = new ProcessStartInfo
            {
                FileName = $"{EasyTierMetadata.EasyTierFilePath}\\easytier-core.exe",
                WorkingDirectory = EasyTierMetadata.EasyTierFilePath,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        var args = new ArgumentsBuilder();

        args.AddFlag("no-tun")
            .AddFlag("multi-thread")
            .AddFlag("enable-kcp-proxy")
            .AddFlag("enable-quic-proxy")
            .AddFlagIf(!Config.Link.TryPunchSym, "disable-sys-hole-punching")
            .AddFlagIf(!Config.Link.EnableIPv6, "disable-ipv6")
            .AddFlagIf(Config.Link.LatencyFirstMode, "latency-first")
            .Add("encryption-algorithm", "aes")
            .Add("compression", "zstd")
            .Add("default-protocol", Config.Link.ProtocolPreference.ToString().ToLowerInvariant())
            .Add("network-name", _lobby.NetworkName)
            .Add("network-secret", _lobby.NetworkSecret)
            .Add("relay-network-whitelist", _lobby.NetworkName)
            .Add("machine-id", Utils.Secret.Identify.LaunchId)
            .Add("rpc-portal", _rpcPort.ToString())
            .Add("private-mode", "true");


        if (asHost)
        {
            args.Add("i", "10.114.51.41")
                .Add("host-name", $"scaffolding-mc-server-{_scfPort}")
                .Add("tcp-whitelist", _scfPort.ToString())
                .Add("tcp-whitelist", _mcPort.ToString())
                .Add("tcp-whitelist", _mcPort.ToString())
                .Add("udp-whitelist", _mcPort.ToString())
                .Add("l", "tcp://0.0.0.0:0")
                .Add("l", "udp://0.0.0.0:0");
        }
        else
        {
            args.AddFlag("d")
                .Add("hostname", Guid.NewGuid().ToString())
                .Add("tcp-whitelist", "0")
                .Add("udp-whitelist", "0")
                .Add("l", "tcp://0.0.0.0:0")
                .Add("l", "udp://0.0.0.0:0");
        }

        foreach (var relay in _GetEyRayList())
        {
            args.Add("p", relay.Url);
        }

        if (Config.Link.RelayType == 1)
        {
            args.AddFlag("disable-p2p");
        }

        return process;
    }

    private IReadOnlyList<ETRelay> _GetEyRayList()
    {
        var relays = ETRelay.RelayList;
        var customedNodes = Config.Link.RelayServer.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var node in customedNodes)
        {
            if (node.Contains("tcp://", StringComparison.OrdinalIgnoreCase) ||
                node.Contains("udp://", StringComparison.OrdinalIgnoreCase))
            {
                relays.Add(new ETRelay
                {
                    Url = node,
                    Name = "Custom",
                    Type = ETRelayType.Custom
                });
            }
            else
            {
                LogWrapper.Warn("EasyTier", $"Invalid cunstomed node URL: {node}.");
            }
        }

        var result = relays.Select(relay => new { relay, serverType = Config.Link.ServerType })
            .Where(rl =>
                (rl.relay.Type == ETRelayType.Selfhosted && rl.serverType != 2) ||
                (rl.relay.Type == ETRelayType.Community && rl.serverType == 1) ||
                rl.relay.Type == ETRelayType.Custom)
            .Select(rl => rl.relay).ToImmutableList();

        return result;
    }

    #region Information

    /// <summary>
    /// Checks the status of EasyTier network until it is ready or time-out.
    /// </summary>
    /// <returns>Returns 0 when the network is ready, otherwise returns 1 for timeout.</returns>
    public async Task<int> CheckEasyTierStatusAsync()
    {
        var retryCount = 0;

        while (_etProcess is null && retryCount < 10)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            retryCount++;
        }

        if (_etProcess is null)
        {
            return 1;
        }

        while (State is not EtState.Ready)
        {
            var info = (await GetPlayersAsync().ConfigureAwait(false)).Players?.FirstOrDefault();
            if (info is null)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                continue;
            }

            if (info.Ping != 1000)
            {
                State = EtState.Ready;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }


        return 0;
    }

    /// <exception cref="ArgumentException">Thrown if host is duplicated.</exception>
    public async Task<EtPlayerList> GetPlayersAsync()
    {
        var cliProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = $"{EasyTierMetadata.EasyTierFilePath}\\easytier-cli.exe",
                WorkingDirectory = EasyTierMetadata.EasyTierFilePath,
                Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} -o json peer",
                ErrorDialog = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                StandardInputEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        try
        {
            cliProcess.Start();
            await cliProcess.WaitForExitAsync().ConfigureAwait(false);

            var output = await cliProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false) +
                         await cliProcess.StandardError.ReadToEndAsync().ConfigureAwait(false);

            if (!cliProcess.HasExited)
            {
                LogWrapper.Warn("EasyTier", "Time outted when trying to get EasyTier peer info.");
            }

            if (JsonNode.Parse(output) is not JsonArray jArray)
            {
                return new EtPlayerList(null, null);
            }


            List<EasyPlayerInfo> players = [];
            EasyPlayerInfo? host = null;
            foreach (var arr in jArray)
            {
                var info = arr.Deserialize<ETPeerInfo>();
                if (info == null)
                {
                    continue;
                }

                if (info.Hostname.StartsWith("scaffolding-mc-server-", StringComparison.Ordinal))
                {
                    if (host is not null)
                    {
                        throw new ArgumentException("Duplicated host.", nameof(host));
                    }

                    host = _ConvertPeerToPlayer(info);
                    continue;
                }

                players.Add(_ConvertPeerToPlayer(info));
            }

            var result = host is not null ? [host, ..players] : players;

            return new EtPlayerList(result, null);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "EasyTier", "Failed to get EasyTier player list info.");
            return new EtPlayerList(null, null);
        }
    }

    private static EasyPlayerInfo _ConvertPeerToPlayer(ETPeerInfo info)
    {
        var playerInfo = new EasyPlayerInfo
        {
            IsHost = info.Hostname.StartsWith("scaffolding-mc-server", StringComparison.Ordinal),
            HostName = info.Hostname,
            Ping = Math.Round(Convert.ToDouble(info.Ping != "-" ? info.Ping : "0")),
            Loss = Math.Round(Convert.ToDouble(info.Loss != "-" ? info.Loss.Replace("%", "") : "0")),
            NatType = info.NatType,
            EasyTierVer = info.ETVersion
        };

        return playerInfo;
    }

    #endregion
}

public record EtPlayerList(IReadOnlyList<EasyPlayerInfo>? Players, EasyPlayerInfo? Local);