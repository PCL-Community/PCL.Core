using System;
using System.Net.Sockets;
using System.Threading;
using PCL.Core.Logging;
using PCL.Core.Net;

namespace PCL.Core.Link.Scaffolding;

public class ScfHelper : ILinkHelper
{
    public string Name => "Scaffolding";
    public Socket? Socket { get; set; }
    public CancellationTokenSource _ctx { get; set; }

    public int Launch()
    {
        throw new System.NotImplementedException();
    }
    public int Close()
    {
        try
        {
            Socket?.SafeClose();
            _ctx?.Cancel();
            return 0;
        }
        catch (Exception ex)
        {
            return 1;
        }
    }

    ~ScfHelper()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
    }
    
    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        GC.SuppressFinalize(this);
        Close();
    }
}