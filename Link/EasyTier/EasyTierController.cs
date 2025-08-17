using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using PCL.Core.Utils.Exts;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.ProgramSetup;
using static PCL.Core.Link.EasyTier.EasyTierInfoProvider;
using static PCL.Core.Link.Lobby.LobbyInfoProvider;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using PCL.Core.Utils.Secret;

namespace PCL.Core.Link.EasyTier
{
    public static class EasyTierController
    {
        public static Process? ETProcess;
        public static int ETRpcPort;
        public static EasyTierState EasyTierStatus;
        public enum EasyTierState
        {
            Stopped,
            Running,
            Ready
        }

        public static int Precheck()
        {
            var existedET = Process.GetProcessesByName("easytier-core");
            foreach (var p in existedET)
            {
                LogWrapper.Warn("Link", $"发现已有的 EasyTier 实例，可能影响与启动器所用的实例通信: {p.Id}");
            }

            // 检查文件
            LogWrapper.Info("Link", "EasyTier 路径: " + ETPath);
            if (!File.Exists(ETPath + "\\easytier-core.exe") || !File.Exists(ETPath + "\\easytier-cli.exe") || !File.Exists(ETPath + "\\wintun.dll"))
            {
                LogWrapper.Error("Link", "EasyTier 不存在或不完整");
                return 1;
            }

            return 0;
        }

        public static int Launch(bool isHost, string name, string secret, string? hostname = null, int port = 25565)
        {
            try
            {
                if (TargetLobby == null || Precheck() != 0) { return 1; }
                ETProcess = new Process { EnableRaisingEvents = true, StartInfo = new ProcessStartInfo { FileName = $"{ETPath}\\easytier-core.exe", WorkingDirectory = ETPath, WindowStyle = ProcessWindowStyle.Hidden } };

                string arguments;

                // 大厅信息
                string lobbyId;
                switch (TargetLobby.Type)
                {
                    case LobbyType.PCLCE:
                        lobbyId = (name + secret + port).ToString().FromB10ToB32();
                        name = ETNetworkNamePrefix + name;
                        secret = ETNetworkSecretPrefix + secret;
                        break;
                    case LobbyType.Terracotta:
                        lobbyId = TargetLobby.OriginalCode;
                        name = "terracotta-mc-" + name;
                        break;
                    default:
                        throw new NotSupportedException("不支持的大厅类型: " + TargetLobby.Type);
                }

                // 网络参数
                string? ip = null;
                if (isHost)
                {
                    LogWrapper.Info("Link", $"本机作为创建者创建大厅，EasyTier 网络名称: {name}");
                    arguments = $"-i 10.114.51.41 --network-name {name} --network-secret {secret} --no-tun --relay-network-whitelist \"{name}\" --private-mode true --tcp-whitelist {port} --udp-whitelist {port}";
                }
                else
                {
                    LogWrapper.Info("Link", $"本机作为加入者加入大厅，EasyTier 网络名称: {name}");
                    arguments = $"-d --network-name {name} --network-secret {secret} --no-tun --relay-network-whitelist \"{name}\" --private-mode true --tcp-whitelist 0 --udp-whitelist 0";
                    switch (TargetLobby.Type)
                    {
                        case LobbyType.PCLCE:
                            ip = "10.114.51.41";
                            break;
                        case LobbyType.Terracotta:
                            ip = "10.144.144.1";
                            break;
                        default:
                            throw new NotSupportedException("不支持的大厅类型: " + TargetLobby.Type);
                    }
                    JoinerLocalPort = NetworkHelper.NewTcpPort();
                    LogWrapper.Info("Link", $"ET 端口转发: 远程 {port} -> 本地 {JoinerLocalPort}");
                    arguments += $" --port-forward=tcp://127.0.0.1:{JoinerLocalPort}/{ip}:{port}"; // TCP
                    arguments += $" --port-forward=udp://127.0.0.1:{JoinerLocalPort}/{ip}:{port}"; // UDP
                }

                // 节点设置
                List<EasyTierRelay.ETRelay> relays = EasyTierRelay.RelayList;
                string customNodes = Setup.Link.RelayServer;
                foreach (string node in customNodes.Split([';'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (node.Contains("tcp://") || node.Contains("udp://"))
                    {
                        relays.Add(new EasyTierRelay.ETRelay
                        {
                            Url = node,
                            Name = "Custom",
                            Type = EasyTierRelay.ETRelayType.Custom
                        });
                    }
                    else
                    {
                        LogWrapper.Warn("Link", $"无效的自定义节点 URL: {node}");
                    }
                }
                foreach (var relay in relays)
                {
                    int serverType = Setup.Link.ServerType;
                    if ((relay.Type == EasyTierRelay.ETRelayType.Selfhosted && serverType != 2) || (relay.Type == EasyTierRelay.ETRelayType.Community && serverType == 1) || relay.Type == EasyTierRelay.ETRelayType.Custom)
                    {
                        arguments += $" -p {relay.Url}";
                    }
                }

                // 中继行为设置
                if (Setup.Link.RelayType == 1)
                {
                    arguments += " --disable-p2p";
                }

                // 数据流代理设置
                arguments += Setup.Link.ProxyType switch
                {
                    0 => " --enable-quic-proxy",
                    1 => " --enable-kcp-proxy",
                    _ => " --enable-quic-proxy --enable-kcp-proxy",
                };

                // 用户名与其他参数
                arguments += " --latency-first --compression=zstd --multi-thread";
                // TODO: 等待玩家档案迁移以获取正在使用的档案名称
                string? showName = "default";
                if (AllowCustomName && !string.IsNullOrWhiteSpace(Setup.Link.Username))
                {
                    showName = Setup.Link.Username;
                }
                else if (NaidProfile != null && !string.IsNullOrWhiteSpace(NaidProfile.Username))
                {
                    showName = NaidProfile.Username;
                }
                arguments += $" --hostname \"{(isHost ? "H|" : "J|") + showName + (!string.IsNullOrWhiteSpace(hostname) ? "|" + hostname : "")}\"";

                // 指定 RPC 端口以避免与其他 ET 实例冲突
                ETRpcPort = NetworkHelper.NewTcpPort();
                arguments += $" --rpc-portal 127.0.0.1:{ETRpcPort}";

                // 启动
                ETProcess.StartInfo.Arguments = arguments;
                LogWrapper.Info("Link", "启动 EasyTier");
                // 操作 UI 显示大厅编号（可能写到 XAML 下面 UI 控制那部分去？）
                ETProcess.Start();
                EasyTierStatus = EasyTierState.Running;
                return 0;
            }
            catch (Exception ex)
            {
                LogWrapper.Error("Link", "尝试启动 EasyTier 时遇到问题: " + ex.ToString());
                EasyTierStatus = EasyTierState.Stopped;
                ETProcess = null;
                return 1;
            }
        }

        public static void Exit()
        {
            if (!(EasyTierStatus == EasyTierState.Stopped) && ETProcess != null)
            {
                try
                {
                    LogWrapper.Info("Link", $"关闭 EasyTier (PID: {ETProcess.Id})");
                    ETProcess.Kill();
                    ETProcess.WaitForExit(200);
                }
                catch (InvalidOperationException)
                {
                    LogWrapper.Warn("Link", "EasyTier 进程不存在，可能已退出");
                }
                catch (NullReferenceException)
                {
                    LogWrapper.Warn("Link", "EasyTier 进程不存在，可能已退出");
                }
                catch (Exception ex)
                {
                    LogWrapper.Error("Link", "关闭 EasyTier 时遇到问题: " + ex.ToString());
                }
                finally
                {
                    EasyTierStatus = EasyTierState.Stopped;
                    ETProcess = null;
                    TargetLobby = null;
                }
            }
        }
    }
}
