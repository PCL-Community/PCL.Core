using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using PCL.Core.Logging;
using PCL.Core.Net;
using Polly;

namespace PCL.Core.App;

[LifecycleService(LifecycleState.Running)]
public sealed class NetworkService : GeneralService {

    // 上下文
    private static LifecycleContext? _context;
    private static LifecycleContext Context => _context!;
    private static ServiceProvider? _provider;
    private static IHttpClientFactory? _factory = null;

    // 继承父类构造函数，此处 `asyncStart` 参数默认值为 `true`，因此省略
    private NetworkService() : base("netowork", "网络服务") { _context = ServiceContext; }

    // 开始事件，可以不写 (虽然但是不写的话整这服务有啥用...)
    public override void Start()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("NetworkServices").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
            {
                UseProxy = true,
                AutomaticDecompression =
                    DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None,
                Proxy = HttpProxyManager.Instance
            }
        );
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IHttpClientFactory>();
        
    }

    // 结束事件，可以不写
    public override void Stop() {
        _provider?.Dispose();
    }

    /// <summary>
    /// 获取 HttpClient
    /// </summary>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient GetClient()
    {
        return _factory.CreateClient("NetworkServices") ?? throw new InvalidOperationException("在初始化完成前的意外的调用");
    }
    

    private static TimeSpan _DefaultPolicy(int retry)
    {
        return TimeSpan.FromMilliseconds(retry * 150 + 150);
    }
    public static AsyncPolicy GetRetryPolicy(int retry,Func<int,TimeSpan>? retryPolicy = null)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retry,
                attempt => retryPolicy is null  
                    ? _DefaultPolicy(attempt):retryPolicy(attempt),
                onRetryAsync: async (exception, timeSpan, retryCount, context) =>
                {
                    LogWrapper.Warn(exception, "Http", $"发送可重试的网络请求失败。");
                    await Task.CompletedTask;
                });
    }

}