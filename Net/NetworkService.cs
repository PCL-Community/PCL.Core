using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using PCL.Core.App;
using PCL.Core.Logging;
using Polly;

namespace PCL.Core.Net;

[LifecycleService(LifecycleState.Loading)]
public sealed class NetworkService : GeneralService {

    private static ServiceProvider? _provider;
    private static IHttpClientFactory? _factory = null;


    private NetworkService() : base("netowork", "网络服务") {}

    public override void Start()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("NetworkServices").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = true,
                AutomaticDecompression =
                    DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None,
                Proxy = HttpProxyManager.Instance
            }
        );
        services.AddHttpClient("CookieClient").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = true,
                AutomaticDecompression =
                    DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None,
                Proxy = HttpProxyManager.Instance,
                UseCookies = false
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
    /// <returns>HttpClient 实例</returns>
    public static HttpClient GetClient(bool useCookie = false)
    {
        return _factory?.CreateClient(useCookie ? "CookieClient":"NetworkServices") ??
         throw new InvalidOperationException("在初始化完成前的意外调用");
    }
    

    private static TimeSpan _DefaultPolicy(int retry)
    {
        return TimeSpan.FromMilliseconds(retry * 150 + 150);
    }
    /// <summary>
    /// 获取重试策略
    /// </summary>
    /// <param name="retry">最大重试次数</param>
    /// <param name="retryPolicy">定义重试器行为</param>
    /// <returns>AsyncPolicy</returns>
    public static AsyncPolicy GetRetryPolicy(int? retry = null,Func<int,TimeSpan>? retryPolicy = null)
    {
        return Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retry ?? 3,
                attempt => retryPolicy?.Invoke(attempt) ?? _DefaultPolicy(attempt),
                onRetryAsync: async (exception, _, _, _) =>
                {
                    LogWrapper.Error(exception, "Http", "发送可重试的网络请求失败。");
                    await Task.CompletedTask;
                });
    }

}