using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PCL.Core.Utils.Net;

public static class HttpClientManager
{
    private static readonly HttpProxyManager ProxyManager = new HttpProxyManager();
    private static HttpClientHandler CurrentHandler = new HttpClientHandler()
    {
        UseProxy = true,
        Proxy = ProxyManager
    };
    private static HttpClient CurrentClient = new HttpClient(CurrentHandler);
    private static HttpClientHandler? PreviousHandler = null;
    private static HttpClient? PreviousClient = null;
    private static readonly object OperationLock = new object();
    
    public static HttpClient GetClient()
    {
        lock (OperationLock)
        {
            return CurrentClient;
        }
    }

    public static void DisposeClient()
    {
        lock (OperationLock)
        {
            CurrentClient?.Dispose();
            CurrentHandler?.Dispose();
            PreviousClient?.Dispose();
            PreviousHandler?.Dispose();
        }
    }

    public static void RefreshClient()
    {
        lock (OperationLock)
        {
            PreviousClient?.Dispose();
            PreviousHandler?.Dispose();
            PreviousClient = CurrentClient;
            PreviousHandler = CurrentHandler;
            CurrentHandler = new HttpClientHandler();
            CurrentClient = new HttpClient(CurrentHandler);
        }
    }
}
    