using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Core.Net.Http.Server;

public abstract class HttpServer : IDisposable
{
    private readonly HttpListener _server = new();
    public readonly ushort Port;
    public readonly string[] Host;

    private Task? HandleLoop;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Dictionary<string, Func<HttpListenerRequest, Task<HttpListenerResponse>>> _handlers = new();

    protected HttpServer(IPAddress[] listenAddr, ushort port = 0)
    {
        // Check parameters
        ArgumentNullException.ThrowIfNull(listenAddr);

        // Resolve port
        if (port == 0) port = (ushort)NetworkHelper.NewTcpPort();
        Port = port;

        // Resolve host
        if (listenAddr.Length == 0)
            listenAddr = [IPAddress.Loopback, IPAddress.IPv6Loopback];

        var hosts = new List<string>();
        foreach (var address in listenAddr)
        {
            _server.Prefixes.Add($"http://{address}:{port}/");
            hosts.Add(address.ToString());
        }
        Host = hosts.ToArray();
    }


    public void Start()
    {
        _server.Start();
        HandleLoop = _handleRequest();

    }

    private async Task _handleRequest()
    {
        while (!(_cancellationTokenSource?.IsCancellationRequested ?? true))
        {
            var incomeContext = await _server.GetContextAsync();
            var path = incomeContext.Request.Url?.AbsolutePath ?? string.Empty;

            if (_handlers.TryGetValue(path, out var handler))
            {
                var response = await handler(incomeContext.Request);
                incomeContext.Response.StatusCode = response.StatusCode;
                incomeContext.Response.ContentType = response.ContentType;
                incomeContext.Response.ContentEncoding = response.ContentEncoding;
                incomeContext.Response.ContentLength64 = response.ContentLength64;
                await incomeContext.Response.OutputStream.CopyToAsync(response.OutputStream);
            }
            else
            {
                incomeContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }



    protected void Route(Func<HttpListenerRequest, Task<HttpListenerResponse>> handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var routeMeta = handle.GetMethodInfo().GetCustomAttribute<HttpRoute>();
        if (routeMeta == null)
            throw new NullReferenceException("Route attribute is missing");

        _handlers[routeMeta.Path] = handle;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        // Stop http listener
        _server.Stop();
        _server.Close();
    }
}