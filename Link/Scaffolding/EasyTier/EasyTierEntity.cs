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
    public readonly int McPort;
    private readonly int _scfPort;

    public int ForwardPort { get; private set; }
    public EtState State { get; private set; }
    public LobbyInfo Lobby  => _lobby;

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
        McPort = mcPort;
        _scfPort = scfPort;
        State = EtState.Stopped;

        var existEntities = Process.GetProcessesByName("easytier-core");
        foreach (var entity in existEntities)
        {
            LogWrapper.Warn("EasyTier", $"Find exist EasyTier Entity, may affect something: {entity.Id}");
        }

        LogWrapper.Info("EasyTier", $"EasyTier folder path: {EasyTierMetadata.EasyTierFilePath}");
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
    /// Launches EasyTier process.
    /// </summary>
    /// <returns>
    /// - 1 means failed to launch EasyTier and can never launch again.<br/>
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
            LogWrapper.Error(ex, "EasyTier", "Failed to launch EasyTier.");
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
            .Add("encryption-algorithm", "aes-gcm")
            .Add("compression", "zstd")
            .Add("default-protocol", Config.Link.ProtocolPreference.ToString().ToLowerInvariant())
            .Add("network-name", _lobby.NetworkName)
            .Add("network-secret", _lobby.NetworkSecret)
            .Add("relay-network-whitelist", _lobby.NetworkName)
            .Add("machine-id", Utils.Secret.Identify.LaunchId)
            .Add("rpc-portal", _rpcPort.ToString());


        if (asHost)
        {
            args.Add("i", "10.114.51.41")
                .Add("host-name", $"scaffolding-mc-server-{_scfPort}")
                .Add("tcp-whitelist", _scfPort.ToString())
                .Add("udp-whitelist", _scfPort.ToString())
                .Add("tcp-whitelist", McPort.ToString())
                .Add("udp-whitelist", McPort.ToString())
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

        foreach (var relay in _GetEtRelayList())
        {
            args.Add("p", relay.Url);
        }

        if (Config.Link.RelayType == 1)
        {
            args.AddFlag("disable-p2p");
        }

        if (!Config.Link.RelayForOthers)
        {
            args.Add("private-mode", "true");
        }
        
        process.StartInfo.Arguments = args.GetResult();
        
        LogWrapper.Debug("EasyTier", process.StartInfo.Arguments);
        
        return process;
    }

    private IReadOnlyList<ETRelay> _GetEtRelayList()
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
                LogWrapper.Warn("EasyTier", $"Invalid custom node URL: {node}.");
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

    /// <summary>
    /// Add a port forward to the EasyTier instance.
    /// </summary>
    /// <param name="targetIp">Remote IP</param>
    /// <param name="targetPort">Remote Port</param>
    /// <returns>Forwarded local port</returns>
    public async Task<int> AddPortForward(string targetIp, int targetPort)
    {
        var localPort = NetworkHelper.NewTcpPort();
        var cliProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = $"{EasyTierMetadata.EasyTierFilePath}\\easytier-cli.exe",
                WorkingDirectory = EasyTierMetadata.EasyTierFilePath,
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
            cliProcess.StartInfo.Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} port-forward add tcp 0.0.0.0:{localPort} {targetIp}:{targetPort}";
            cliProcess.Start();
            await cliProcess.WaitForExitAsync().ConfigureAwait(false);
            
            LogWrapper.Debug("ET Cli", await cliProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false) +
                                       await cliProcess.StandardError.ReadToEndAsync().ConfigureAwait(false));
            
            cliProcess.StartInfo.Arguments = $"--rpc-portal 127.0.0.1:{_rpcPort} port-forward add udp 0.0.0.0:{localPort} {targetIp}:{targetPort}";
            cliProcess.Start();
            await cliProcess.WaitForExitAsync().ConfigureAwait(false);
            
            LogWrapper.Debug("ET Cli", await cliProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false) +
                                       await cliProcess.StandardError.ReadToEndAsync().ConfigureAwait(false));
        }
        catch (Exception e)
        {
            LogWrapper.Error(e, "ET Cli", "Failed to add port forward.");
        }
        return localPort;
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
            
            LogWrapper.Debug("[ET Cli] " + output);

            if (!cliProcess.HasExited)
            {
                LogWrapper.Warn("EasyTier", "Timeout when trying to get EasyTier peer info.");
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
            Ip = info.Ipv4,
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