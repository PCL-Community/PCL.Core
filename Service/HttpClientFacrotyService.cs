using System;
using System.IO;
using System.Threading;
using PCL.Core.Helper;
using PCL.Core.LifecycleManagement;
using PCL.Core.Utils.Logger;
using PCL.Core.Utils.Net;

namespace PCL.Core.Service;

[LifecycleService(LifecycleState.Loading, Priority = int.MaxValue)]
public class HttpClientFactory : ILifecycleService
{
    public string Identifier => "Network";
    public string Name => "网络服务";
    public bool SupportAsyncStart => true;
    
    private readonly LifecycleContext Context;
    private HttpClientFactory() { Context = Lifecycle.GetContext(this); }
    
    public void Start()
    {
        Context.Trace("正在初始化 HttpClientFactory");
        NativeInterop.RunInNewThread(() =>
            {
                try
                {
                    while (true)
                    {
                        Thread.Sleep(TimeSpan.FromHours(4));
                        HttpClientManager.RefreshClient();
                    }
                }
                catch (ThreadInterruptedException ex)
                {
                    return;
                }
            }
        );
    }

    public void Stop()
    {
        HttpClientManager.DisposeClient();
    }
}