using System;
using System.Net.Http;
using System.Threading;
using PCL.Core.Helper;
using PCL.Core.LifecycleManagement;
using PCL.Core.Utils;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading)]
public class HttpClientService : ILifecycleService
{
    public string Identifier => "network";
    public string Name => "网络服务";
    public bool SupportAsyncStart => true;

    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private HttpClientService() { _context = Lifecycle.GetContext(this); }

    public static HttpProxyManager Proxy { get; } = new();
    
    private static HttpClientHandler _currentHandler = new()
    {
        UseProxy = true,
        Proxy = Proxy
    };

    private static HttpClient _currentClient = new(_currentHandler);

    private static readonly object _OperationLock = new();

    public static HttpClient GetClient()
    {
        lock (_OperationLock)
        {
            return _currentClient;
        }
    }

    private static HttpClientHandler? _previousHandler;
    private static HttpClient? _previousClient;

    private static void _RefreshClient()
    {
        Context.Debug("正在刷新 HttpClient"); 
        _previousClient?.Dispose(); 
        _previousHandler?.Dispose(); 
        _previousClient = _currentClient; 
        _previousHandler = _currentHandler;
        _currentHandler = new HttpClientHandler
        {
            UseProxy = true,
            Proxy = Proxy
        };
        _currentClient = new HttpClient(_currentHandler); 
        Context.Trace("已刷新 HttpClient");
    }

    private static readonly AutoResetEvent _RefreshEvent = new(false);
    private static readonly AutoResetEvent _RefreshFinishedEvent = new(false);

    public static void RefreshClient()
    {
        lock (_OperationLock)
        {
            _manualRefresh = true;
            _RefreshEvent.Set();
            _RefreshFinishedEvent.WaitOne();
        }
    }

    private static bool _stopped = false;
    private static bool _manualRefresh = false;

    public void Start()
    {
        Context.Trace("正在初始化 HttpClient 服务");
        NativeInterop.RunInNewThread(() => {
            while (true)
            {
                _RefreshEvent.WaitOne(TimeSpan.FromHours(4));
                if (_stopped) break;
                _RefreshClient();
                if (_manualRefresh)
                {
                    _manualRefresh = false;
                    _RefreshFinishedEvent.Set();
                }
            }
        }, "HttpClient Refresh");
    }

    public void Stop()
    {
        lock (_OperationLock)
        {
            _stopped = true;
            _RefreshEvent.Set();
            _currentClient.Dispose();
            _currentHandler.Dispose();
            _previousClient?.Dispose();
            _previousHandler?.Dispose();
        }
    }
}
