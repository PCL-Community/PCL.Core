using System;
using System.Net.Sockets;
using System.Threading;

namespace PCL.Core.Link;

public interface ILinkHelper : IDisposable
{
    public string Name { get; }
    protected Socket? Socket { get; set; }
    protected CancellationTokenSource? _ctx { get; set; }
    public int Launch();
    public int Close();
}