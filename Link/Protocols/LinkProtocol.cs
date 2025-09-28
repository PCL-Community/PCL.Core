using System;
using System.Net;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Link.Protocols;

public abstract class LinkProtocol(bool isServer) : IDisposable
{
    private const string Identifier = "none";
    private readonly CancellationTokenSource _ctx = new();
    private readonly TcpHelper _tcpHelper = new(isServer);

    public int Launch()
    {
        if (isServer)
        {
            _tcpHelper.ReceivedData += ReceivedData;
            _tcpHelper.ClientDisconnected += ClientDisconnected;
            return _tcpHelper.Launch();
        }
        else
        {
            var res = _tcpHelper.Launch();
            _ = Task.Run(() => ClientPart(_ctx.Token), _ctx.Token);
            return res;
        }
    }
    
    protected abstract void ReceivedData(object? sender, TcpHelper.ReceivedDateEventArgs e);
    protected abstract void ClientDisconnected(object? sender, EventArgs e);
    protected abstract Task ClientPart(CancellationToken token);

    public int Close()
    {
        try
        {
            _ctx.Cancel();
            _tcpHelper.Close();
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