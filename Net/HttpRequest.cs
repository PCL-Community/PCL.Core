using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public static class HttpRequest
{
    private static readonly HttpClient _Client = new(new HttpClientHandler()
    {
        UseProxy = true,
        Proxy = HttpProxyManager.Instance
    }); 
    public static async Task<HttpResponseMessage> TryGetServerResponse(HttpRequestOptions options)
    {
        Exception? lastException = null;
        while (options.Retry >0)
        {
            try
            {
                using var request = options.GetRequestMessage();
                using var cts = new CancellationTokenSource(options.Timeout);
                return await _Client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                options.Retry--;
                LogWrapper.Error(ex, "Network", "发送可重试的网络请求失败");
            }
        }

        throw new HttpRequestException("发送网络请求失败", lastException);
    }

    public static async Task<HttpResponseMessage> GetServerResponse(HttpRequestOptions options)
    {
        HttpResponseMessage response = await TryGetServerResponse(options);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        return await _Client.SendAsync(request);
    }
    public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,HttpCompletionOption options)
    {
        return await _Client.SendAsync(request, options);
    }
    public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,HttpCompletionOption options,CancellationToken token)
    {
        return await _Client.SendAsync(request, options,token);
    }
}
