using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public class HttpRequestBuilder
{
    private readonly HttpRequestMessage _request;
    private readonly Dictionary<string, string> _cookies = [];
    private HttpCompletionOption _completionOption = HttpCompletionOption.ResponseContentRead;
    private bool _doLog = true;

    public HttpRequestBuilder(string url, HttpMethod method)
    {
        var uriData = new Uri(url);
        _request = new HttpRequestMessage(method, uriData);
    }

    /// <summary>
    /// 创建一个 HttpRequestBuilder 对象
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="method">HTTP 方法</param>
    /// <returns>HttpRequestBuilder</returns>
    public static HttpRequestBuilder Create(string url, HttpMethod method)
    {
        return new HttpRequestBuilder(url, method);
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
    /// 设置一个请求所用的 Cookie，如果已设置过对应的键，则旧的会被覆盖
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithCookie(string key, string value)
    {
        _cookies[key] = value;
        return this;
    }

    /// <summary>
    /// 设置多个请求所用的 Cookie，如果已设置过对应的键，则旧的会被覆盖
    /// </summary>
    /// <param name="cookies"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithCookie(IDictionary<string, string> cookies)
    {
        foreach (var cookie in cookies)
        {
            _cookies[cookie.Key] = cookie.Value;
        }

        return this;
    }

    /// <summary>
    /// 设置多个 Header
    /// </summary>
    /// <param name="headers"></param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithHeader(IDictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            _request.Headers.TryAddWithoutValidation(header.Key,header.Value);
        }

        return this;
    }

    /// <summary>
    /// 设置一个 Header
    /// </summary>
    /// <param name="key">Header Name</param>
    /// <param name="value">Header Value</param>
    /// <returns>HttpRequestBuilder</returns>
    public HttpRequestBuilder WithHeader(string key, string value)
    {
        if (key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) && _request.Content is not null)
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
    /// 设置一个 Header
    /// </summary>
    /// <param name="header"></param>
    /// <returns></returns>
    public HttpRequestBuilder WithHeader(KeyValuePair<string, string> header) => WithHeader(header.Key, header.Value);

    public HttpRequestBuilder WithAuthentication(string scheme, string token)
    {
        if (string.IsNullOrEmpty(scheme))
            throw new ArgumentNullException(nameof(scheme));

        if (string.IsNullOrEmpty(token))
            throw new ArgumentNullException(nameof(token));

        _request.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);
        return this;
    }

    // 快捷方法
    public HttpRequestBuilder WithBearerToken(string token) => WithAuthentication("Bearer", token);

    public HttpRequestBuilder WithHttpVersion(Version version)
    {
        _request.Version = version;
        return this;
    }

    public HttpRequestBuilder WithLoggingOptions(bool doLog)
    {
        _doLog = doLog;
        return this;
    }

    public HttpRequestBuilder WithCompletionOption(HttpCompletionOption option)
    {
        _completionOption = option;
        return this;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="throwIfNotSuccess">请求失败时是否抛出异常</param>
    /// <param name="retryTimes">请求重试次数</param>
    /// <param name="retryPolicy">依据请求当前尝试的次数给出的重试时长控制方法</param>
    /// <exception cref="HttpRequestException">要求 <paramref name="throwIfNotSuccess"/> 时并且 HTTP 请求失败</exception>
    /// <returns></returns>
    public async Task<HttpResponseHandler> SendAsync(bool throwIfNotSuccess = false, int retryTimes = 3, Func<int,TimeSpan>? retryPolicy = null)
    {
        // 处理 Cookies
        if (_cookies.Count != 0)
        {
            if (_request.Headers.Contains("Cookie")) _request.Headers.Remove("Cookie"); //去掉野生的饼干
            var cookiesCtx = new StringBuilder(_cookies.Count * 30); //后期需要根据实际使用调整预分配的容量大小以提高文本构建性能
            foreach (var cookie in _cookies)
            {
                if (cookiesCtx.Length > 0)
                    cookiesCtx.Append("; ");
                cookiesCtx
                    .Append(Uri.EscapeDataString(cookie.Key))
                    .Append('=')
                    .Append(_getSafeCookieValue(cookie.Value));
            }
            _request.Headers.TryAddWithoutValidation("Cookie", cookiesCtx.ToString());
        }

        var client = NetworkService.GetClient();
        _makeLog($"向 {_request.RequestUri} 发起 {_request.Method} 请求");
        var responseMessage = await NetworkService.GetRetryPolicy(retryTimes, retryPolicy)
            .ExecuteAsync(async () => await client.SendAsync(_request, _completionOption));
        var responseUri = responseMessage.RequestMessage?.RequestUri;
        if (responseUri != null && _request.RequestUri != responseUri) _makeLog($"已重定向至 {responseUri}");
        _makeLog($"已获取请求结果，返回 HTTP 状态码: {responseMessage.StatusCode}");
        if (throwIfNotSuccess) responseMessage.EnsureSuccessStatusCode();
        return new HttpResponseHandler(responseMessage);
    }

    private void _makeLog(string msg)
    {
        if (!_doLog) return;
        LogWrapper.Info("Network", msg);
    }

    private static string _getSafeCookieValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var needsEncoding = value.Any(c => _ForbiddenCookieValueChar.Contains(c) || char.IsControl(c));
        return needsEncoding ? Uri.EscapeDataString(value) : value;
    }

    private static readonly char[] _ForbiddenCookieValueChar =
    [
        ';', ',', ' ', '\r', '\n', '\t', '\0',
        '=', '"', '\'', '\\', '<', '>'
    ];
}