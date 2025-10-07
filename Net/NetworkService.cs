using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Authentication;
using PCL.Core.App;
using PCL.Core.Logging;
using Polly;

namespace PCL.Core.Net;

[LifecycleService(LifecycleState.Loading)]
public sealed class NetworkService : GeneralService {

    private static ServiceProvider? _provider;
    private static IHttpClientFactory? _factory;

    private NetworkService() : base("network", "网络服务") {}

    public override void Start()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = true,
                AutomaticDecompression = DecompressionMethods.All, //在这里添加 None 的给我重学二进制去 😡
                SslProtocols = SslProtocols.None,
                Proxy = HttpProxyManager.Instance,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 25,
                UseCookies = false, //禁止自动 Cookie 管理
                MaxConnectionsPerServer = 64,
            }
        );
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IHttpClientFactory>();
        
    }

    public override void Stop() {
        _provider?.Dispose();
    }

    /// <summary>
    /// 获取 HttpClient
    /// </summary>
    /// <param name="wantClientType">指定要求的 HttpClient 来源</param>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient GetClient(string wantClientType = "default")
    {
        return _factory?.CreateClient(wantClientType) ??
               throw new InvalidOperationException("在初始化完成前的意外调用");
    }
    

    private static TimeSpan _DefaultPolicy(int retry)
    {
        return TimeSpan.FromMilliseconds(retry * 6_000 + 10_000);
    }
    /// <summary>
    /// 获取重试策略
    /// </summary>
    /// <param name="retry">最大重试次数</param>
    /// <param name="retryPolicy">定义重试器行为</param>
    /// <returns>AsyncPolicy</returns>
    public static AsyncPolicy GetRetryPolicy(int retry = 3, Func<int,TimeSpan>? retryPolicy = null)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retry,
                attempt => retryPolicy?.Invoke(attempt) ?? _DefaultPolicy(attempt),
                onRetryAsync: (exception, _, _, _) =>
                {
                    LogWrapper.Error(exception, "Http", "发送可重试的网络请求失败。");
                    return Task.CompletedTask;
                });
    }

}