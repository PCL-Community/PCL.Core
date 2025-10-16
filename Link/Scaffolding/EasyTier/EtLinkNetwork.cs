using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Link.Interop.ControlLayer;
using PCL.Core.Link.Interop.NetworkLayer;
using PCL.Core.Link.Scaffolding.Utils;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils;
using PCL.Core.Utils.Hash;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Link.Scaffolding.EasyTier;

public class EtLinkNetwork(string workDirectory) : ILinkNetworkSession
{
    private List<EtLinkPeer> _peers = [];
    private Process? _easyTier;
    private int _easyTierPrcPort;
    private CancellationTokenSource? _cts;
    private const string ModuleName = "EasyTierController";

    private async ValueTask<bool> _checkEasyTierAsync()
    {
        var coreApp = Path.Combine(workDirectory, "easytier-core.exe");
        var cliApp = Path.Combine(workDirectory, "easytier-cli.exe");
        return File.Exists(coreApp) && File.Exists(cliApp);
    }

    public async ValueTask<string> CreateSession(ILinkPeer creatorLinkPeer)
    {
        if (await _checkEasyTierAsync()) throw new FileNotFoundException("EasyTier app is not installed");

        // Check if there is a EasyTier running
        if (_easyTier is not null && !_easyTier.HasExited)
        {
            LogWrapper.Warn(ModuleName, $"EasyTier is running, try to kill the process: {_easyTier.Id}");
            try
            {
                _easyTier.Kill(true);
            }
            catch (Exception e)
            {
                LogWrapper.Error(e, ModuleName, "Failed to kill EasyTier, continue creating new instance.");
            }
            _easyTier = null;
        }

        // Build a new EasyTier instance
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(workDirectory, "easytier-core.exe"),
            WindowStyle = ProcessWindowStyle.Hidden
        };

        // Set Environment
        var etEnv = startInfo.Environment;
        var roomInfo = RoomId.GenerateRandomRoomId();
        // Basic network
        etEnv["ET_NETWORK_NAME"] = roomInfo.GetNetworkName();
        etEnv["ET_NETWORK_SECRET"] = roomInfo.GetNetworkSecret();
        // Network behavior
        etEnv["ET_BIND_DEVICE"] = "true";              // Bind to physical device other than virtual device
        etEnv["ET_NO_TUN"] = "true";                   // Disable TUN
        etEnv["ET_MULTI_THREAD"] = "true";             // Multi-thread
        etEnv["ET_DHCP"] = "true";                     // Set DHCP to auto set IP
        etEnv["ET_USE_SMOLTCP"] = "true";              // Use SmolTcp
        etEnv["ET_COMPRESSION"] = "zstd";              // Use Zstd Compression
        etEnv["ET_ENCRYPTION_ALGORITHM"] = "chacha20"; // Use ChaCha20 for encryption
        etEnv["ET_ENABLE_KCP_PROXY"] = "true";         // Use KCP for Tcp stack
        etEnv["ET_ENABLE_QUIC_PROXY"] = "true";        // Use QUIC for Tcp stack (not work in the port forward mode)
        etEnv["ET_PRIVATE_MODE"] = "true";             // Private mode, do not allow packet not from our network
        etEnv["ET_RELAY_ALL_PEER_RPC"] = "true";       // Relay all peer's RPC, helping others to punch
        if (!Config.Link.EnableIPv6) etEnv["ET_DISABLE_IPV6"] = "true";
        if (Config.Link.LatencyFirstMode) etEnv["ET_LATENCY_FIRST"] = "true";
        // EasyTier RPC
        _easyTierPrcPort = NetworkHelper.NewTcpPort();
        etEnv["ET_RPC_PORTAL"] = $"127.0.0.1:{_easyTierPrcPort}";
        // EasyTier Protocol
        var protocolPreference = Config.Link.ProtocolPreference;
        var listenAddr = protocolPreference switch
        {
            LinkProtocolPreference.Tcp => $"tcp://0.0.0.0:{NetworkHelper.NewTcpPort()}",
            LinkProtocolPreference.Udp => $"udp://0.0.0.0:{RandomUtils.NextInt(10000, 65535)}",
            _ => throw new ArgumentOutOfRangeException(nameof(protocolPreference), "Unsupported argument")
        };
        etEnv["ET_LISTENERS"] = listenAddr;
        etEnv["ET_DEFAULT_PROTOCOL"] = protocolPreference.ToString().ToLower();

        _cts = new CancellationTokenSource();
        _easyTier = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        if (!_easyTier.Start())
        {
            LogWrapper.Error(ModuleName, "EasyTier startup not success");
            throw new Exception("启动 EasyTier 失败");
        }

        return roomInfo.ToString();
    }

    public async ValueTask<bool> JoinSession(string sessionId, string sessionSecret)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<ILinkPeer> GetPeers()
    {
        return _peers;
    }

    public async ValueTask Shutdown()
    {
        throw new System.NotImplementedException();
    }

    public async ValueTask DisposeAsync()
    {
        await Shutdown();
    }

    private EtLinkPeer _getSelfPeer()
    {
        var self = new EtLinkPeer
        {
            Name = "Local",
            Id = SHA256Provider.Instance.ComputeHash($"PCL-CE|{Identify.LaunchId}|Link|EasyTier"),
            LastHeartbeat = DateTime.Now,
            Status = ConnectionStatus.Local,
            Latency = 0,
            PacketLoss = 0,
            Rx = 0,
            Tx = 0,
            Metadata = new Dictionary<PeerMetadata, object>
            {
                { PeerMetadata.Role, PeerRole.Creator },
                { PeerMetadata.JoinAt, DateTime.Now }
            }
        };
        return self;
    }
}