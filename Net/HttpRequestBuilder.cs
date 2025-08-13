using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PCL.Core.Net;

public class HttpRequestBuilder
{
    private readonly HttpRequestMessage _request;
    private HttpResponseMessage? _response;
    private bool _useCookie;
    
    private HttpRequestBuilder(string url,HttpMethod method)
    {
        _request = new HttpRequestMessage(method,url);
    }
    /// <summary>
    /// 创建一个 HttpRequestBuilder 对象
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="method">HTTP 方法</param>
    /// <returns>HttpRequestBuilder</returns>
    public static HttpRequestBuilder Create(string url,HttpMethod method)
    {
        return new HttpRequestBuilder(url,method);
    }
    /// <summary>
    /// 设置请求载荷
    /// </summary>
    /// <param name="content">请求载荷</param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithContent(HttpContent content)
    {
        _request.Content = content;
        return this;
    }
    /// <summary>
    /// 设置请求使用的 Cookie
    /// </summary>
    /// <param name="cookie">Cookie</param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithCookie(string cookie)
    {
        _useCookie = true;
        _request.Headers.TryAddWithoutValidation("Cookie",cookie);
        return this;
    }
    /// <summary>
    /// 批量设置 Headers
    /// </summary>
    /// <param name="headers">实现了 IDictionary 的 对象</param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithHeaders(IDictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            _request.Headers.Add(kvp.Key,kvp.Value);
        }

        return this;
    }
    /// <summary>
    /// 设置单个 Header
    /// </summary>
    /// <param name="key">Header Name</param>
    /// <param name="value">Header Value</param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithHeader(string key, string value)
    {
        if (key.StartsWith("Content", StringComparison.OrdinalIgnoreCase) && _request.Content is not null)
        {
            _request.Content.Headers.TryAddWithoutValidation(key, value);
        }
        else
        {
            _request.Headers.TryAddWithoutValidation(key, value);
        }

        return this;
    }
    /// <summary>
    /// 获取响应的 HttpResponseMessage 对象，如果请求尚未完成，则返回 null
    /// </summary>
    /// <returns>HttpResponseMessage</returns>
    public HttpResponseMessage GetResponse(
        HttpCompletionOption whenComplete = HttpCompletionOption.ResponseContentRead,
        int retry = 3,
        Func<int, TimeSpan>? retryPolicy = null)
    {
        return _getResponseAsync(whenComplete, retry, retryPolicy).GetAwaiter().GetResult();
    }

    public async Task<HttpResponseMessage> GetResponseAsync(
        HttpCompletionOption whenComplete = HttpCompletionOption.ResponseContentRead,
        int retry = 3,
        Func<int, TimeSpan>? retryPolicy = null)
    {
        return await _getResponseAsync(whenComplete, retry, retryPolicy).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> _getResponseAsync(
        HttpCompletionOption whenComplete,
        int retry,
        Func<int, TimeSpan>? retryPolicy = null)
    {
        using var client = NetworkService.GetClient(_useCookie);
        return await NetworkService.GetRetryPolicy(retry, retryPolicy)
            .ExecuteAsync(async () => await client.SendAsync(_request, whenComplete));
    }
}