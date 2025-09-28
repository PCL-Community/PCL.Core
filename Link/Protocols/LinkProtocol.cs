using System;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Link.Protocols;

/// <summary>
/// 链接协议抽象基类，支持服务器和客户端两种模式
/// </summary>
/// <param name="isServer">是否为服务器模式</param>
public abstract class LinkProtocol(bool isServer) : IDisposable
{
    /// <summary>
    /// 获取协议标识符
    /// </summary>
    protected virtual string Identifier => "none";
    
    /// <summary>
    /// 是否为服务器模式
    /// </summary>
    protected readonly bool _isServer = isServer;
    
    protected readonly CancellationTokenSource _ctx = new();
    protected readonly TcpHelper _tcpHelper = new(isServer);
    protected readonly ConcurrentDictionary<IPEndPoint, byte> _clientDict = new();

    /// <summary>
    /// 启动协议
    /// </summary>
    /// <returns>启动结果代码，0表示成功</returns>
    public int Launch()
    {
        if (_isServer)
        {
            _tcpHelper.ReceivedData += ReceivedData;
            _tcpHelper.AcceptedClient += (s, e) =>
            {
                _clientDict.TryAdd(e.ClientEndPoint, 0); // TODO 添加客户端信息
            };
            _tcpHelper.ClientDisconnected += (s, e) =>
            {
                _clientDict.TryRemove(e.ClientEndPoint, out _);
            };
            LogWrapper.Info($"{Identifier} 服务端已启动");
            return _tcpHelper.Launch();
        }
        else
        {
            var res = _tcpHelper.Launch();
            _ = Task.Run(async () => {
                try
                {
                    await ClientPart(_ctx.Token);
                }
                catch (Exception ex)
                {
                    LogWrapper.Error(ex, "Link", $"客户端 {Identifier} 协议发生错误");
                }
            }, _ctx.Token);
            LogWrapper.Info($"{Identifier} 客户端已启动");
            return res;
        }
    }
    
    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">接收到的数据事件参数</param>
    protected abstract void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e);
    
    /// <summary>
    /// 客户端部分逻辑
    /// </summary>
    /// <param name="token">取消令牌</param>
    /// <returns>任务</returns>
    protected abstract Task ClientPart(CancellationToken token);

    /// <summary>
    /// 关闭协议
    /// </summary>
    /// <returns>关闭结果代码，0表示成功</returns>
    public int Close()
    {
        try
        {
            _ctx.Cancel();
            _tcpHelper.Close();
            _tcpHelper.Dispose();
            _clientDict.Clear();
            LogWrapper.Info($"{Identifier} 协议已关闭");
            return 0;
        }
        catch (Exception ex)
        {
            LogWrapper.Error(ex, "Link", $"关闭 {Identifier} 协议时发生错误");
            return 1;
        }
    }
    
    ~LinkProtocol()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        Close();
    }
}