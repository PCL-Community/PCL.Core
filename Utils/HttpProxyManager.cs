using System;
using System.Net;

namespace PCL.Core.Utils;

public class HttpProxyManager : IWebProxy
{
    private readonly object _operationLock = new();
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

    private WebProxy? _CurrentProxy
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

    private readonly WebProxy _systemProxy = new()
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
        if (_CurrentProxy is not null && !RequireRefresh) return _CurrentProxy;
        RequireRefresh = false;
        if (!string.IsNullOrWhiteSpace(_proxyAddress))
        {
            _CurrentProxy = new WebProxy(_proxyAddress, true);
            return _CurrentProxy;
        }

        _CurrentProxy = _systemProxy;
        return _CurrentProxy;
    }

    public bool IsBypassed(Uri? uri)
    {
        if (DisableProxy) return true;
        return _CurrentProxy?.IsBypassed(uri ?? new Uri("http://127.0.0.1")) ?? true;
    }
}