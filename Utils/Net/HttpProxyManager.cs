using System;
using System.Net;

namespace PCL.Core.Utils.Net;


public class HttpProxyManager : IWebProxy
{
    private object _operationLock = new object();
    private ICredentials? _credentials;

    private string? _proxyAddress;

    public ICredentials? Credentials
    {
        get
        {
            lock (_operationLock)
            {
                return _credentials;
            }
        }
        set
        {
            lock (_operationLock)
            {
                _credentials = value ??
                               throw new ArgumentNullException(nameof(Credentials), $"{nameof(Credentials)} 不能为 Null");
            }
        }
    }

    public string? ProxyAddress
    {
        get
        {
            lock (_operationLock)
            {
                return _proxyAddress;
            }
        }
        set
        {
            lock (_operationLock)
            {
                _proxyAddress = value ?? "";
            }
        }
    }

    private WebProxy? _proxy;

    private WebProxy? CurrentProxy
    {
        get
        {
            lock (_operationLock)
            {
                return _proxy;
            }
        }
        set
        {
            lock (_operationLock)
            {
                _proxy = value;
            }
        }
    }

    public bool DisableProxy { get; set; }

    private WebProxy _systemProxy = new WebProxy()
    {
        BypassProxyOnLocal = true
    };

    public bool RequireRefresh;

    public Uri? GetProxy(Uri? uri)
    {
        return GetWebProxy(uri)?.Address ?? uri;
    }

    public WebProxy? GetWebProxy(Uri? uri)
    {
        if (CurrentProxy is not null && !RequireRefresh) return CurrentProxy;
        RequireRefresh = false;
        if (!string.IsNullOrWhiteSpace(_proxyAddress))
        {
            CurrentProxy = new WebProxy(_proxyAddress, true);
            return CurrentProxy;
        }

        CurrentProxy = _systemProxy;
        return CurrentProxy;
    }

    public bool IsBypassed(Uri? uri)
    {
        if (DisableProxy) return true;
        return CurrentProxy?.IsBypassed(uri ?? new Uri("http://127.0.0.1")) ?? true;
    }
}