using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PCL.Core.Utils.Exts;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Net;
using static PCL.Core.Link.Lobby.LobbyHandler;
using static PCL.Core.Link.Natayark.NatayarkProfileManager;
using PCL.Core.ProgramSetup;

namespace PCL.Core.Link.EasyTier
{
    public class EasyTierController
    {
        public const string ETNetworkNamePrefix = "PCLCELobby";
        public const string ETNetworkSecretPrefix = "PCLCEETLOBBY2025";
        public const string ETVersion = "2.4.1";
        public string ETPath = Path.Combine(FileService.LocalDataPath, "EasyTier", ETVersion, "easytier-windows-" + "x86_64");

        public Process ETProcess;
        public bool IsETRunning = false;
        public bool IsETReady = false;

        public int Launch(bool isHost, string name = null, string secret = null, bool isAfterDownload = false, int port = 25565)
        {
            try
            {
                var etFilePath = $"{ETPath}\\easytier-core.exe";

                var existedET = Process.GetProcessesByName("easytier-core");
                foreach (var p in existedET)
                {
                    LogWrapper.Warn("Link", $"发现已有的 EasyTier 实例，可能影响与启动器所用的实例通信: {p.Id}");
                }

                ETProcess = new Process { EnableRaisingEvents = true, StartInfo = new ProcessStartInfo { FileName = etFilePath, WorkingDirectory = ETPath, WindowStyle = ProcessWindowStyle.Hidden } };

                // 兜底
                if (!File.Exists(ETPath + "\\easytier-core.exe") || !File.Exists(ETPath + "\\easytier-cli.exe") || !File.Exists(ETPath + "\\wintun.dll") && !isAfterDownload) 
                {
                    LogWrapper.Info("Link", "EasyTier 不存在，开始下载");
                }
                LogWrapper.Info("Link", "EasyTier 路径: " + ETPath);

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
                if (isHost)
                {
                    LogWrapper.Info("Link", $"本机作为创建者创建大厅，EasyTier 网络名称: {name}");
                    arguments = $"-i 10.114.51.41 --network-name {name} --network-secret {secret} --no-tun --relay-network-whitelist \"{name}\" --private-mode true --tcp-whitelist {port} --udp-whitelist {port}";
                }
                else
                {
                    LogWrapper.Info("Link", $"本机作为加入者加入大厅，EasyTier 网络名称: {name}");
                    arguments = $"-d --network-name {name} --network-secret {secret} --no-tun --relay-network-whitelist \"{name}\" --private-mode true --tcp-whitelist 0 --udp-whitelist 0";
                    string ip;
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
                switch (Setup.Link.ProxyType)
                {
                    case 0:
                        arguments += " --enable-quic-proxy";
                        break;
                    case 1:
                        arguments += " --enable-kcp-proxy";
                        break;
                    default:
                        arguments += " --enable-quic-proxy --enable-kcp-proxy";
                        break;
                }

                // 用户名与其他参数
                arguments += " --latency-first --compression=zstd --multi-thread";
                string hostname = (isHost ? "H|" : "J|") + NaidProfile.Username;
                // TODO: 等待玩家档案迁移以获取正在使用的档案名称
                arguments += $" --hostname \"{hostname}\"";

                // 启动
                ETProcess.StartInfo.Arguments = arguments;
                LogWrapper.Info("Link", "启动 EasyTier");
                // 操作 UI 显示大厅编号（可能写到 XAML 下面 UI 控制那部分去？）
                ETProcess.Start();
                IsETRunning = true;
                return 0;
            }
            catch (Exception ex)
            {
                LogWrapper.Error("Link", "尝试启动 EasyTier 时遇到问题: " + ex.ToString());
                IsETRunning = false;
                ETProcess = null;
                return 1;
            }
        }

        public void Exit()
        {
            if (IsETRunning && ETProcess != null)
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
                    IsETRunning = false;
                    IsETReady = false;
                    ETProcess = null;
                    TargetLobby = null;
                    // TODO: 清理相关 UI 状态
                    // TODO: 停止端口转发
                }
            }
        }
    }
}
