using System;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Link.Protocols;

/// <summary>
/// 链接协议抽象基类，支持服务器和客户端两种模式
/// </summary>
/// <param name="isServer">是否为服务器模式</param>
public abstract class LinkProtocol(bool isServer, string identifier) : IDisposable
{
    /// <summary>
    /// 获取协议标识符
    /// </summary>
    protected virtual string Identifier => identifier;
    
    /// <summary>
    /// 是否为服务器模式
    /// </summary>
    private readonly bool _isServer = isServer;
    
    protected enum ProtocolState
    {
        Stopped,
        Running
    }
    protected ProtocolState State = ProtocolState.Stopped;
    protected readonly CancellationTokenSource Ctx = new();
    protected readonly TcpHelper TcpHelper = new();

    /// <summary>
    /// 启动协议
    /// </summary>
    /// <returns>启动结果代码，0表示成功</returns>
    public int Launch()
    {
        if (_isServer)
        {
            TcpHelper.ReceivedData += ReceivedData;
            var port = NetworkHelper.NewTcpPort();
            TcpHelper.StartListening(port);
            State = ProtocolState.Running;
            LogWrapper.Info($"{Identifier} 服务端已启动");
        }
        else
        {
            LaunchClientAsync();
        }
        return 0;
    }
    
    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">接收到的数据事件参数</param>
    protected abstract void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e);
    
    /// <summary>
    /// 即客户端部分逻辑。写成抽象方法, 是因为每个协议的客户端逻辑都不一样。
    /// </summary>
    protected abstract Task LaunchClientAsync();

    /// <summary>
    /// 关闭协议
    /// </summary>
    /// <returns>关闭结果代码，0表示成功</returns>
    public virtual int Close()
    {
        try
        {
            Ctx.Cancel();
            TcpHelper.Close();
            TcpHelper.Dispose();
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