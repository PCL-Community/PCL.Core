using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PCL.Core.Net;

public class HttpRequestOptions
{
    private string? _requestUrl;
    public string? RequestUrl
    {
        get => _requestUrl ?? "";
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(RequestUrl), "不能为空或 Null");
            _requestUrl = value;
        }
    }

    public HttpMethod Method { get; set; } = HttpMethod.Get;

    public int Retry { get; set; } = 3;

    public Dictionary<string, string> Headers { get; set; } = new();

    public string ContentType
    {
        get
        {
            Headers.TryGetValue("Content-Type", out var value);
            return value ?? "";
        }
        set => Headers["Content-Type"] = value;
    }

    private HttpContent? _content;
    public HttpContent? Content
    {
        get => _content;
        set => _content = value ?? throw new ArgumentNullException(nameof(Content), "请求载荷不能为 Null");
    }

    public int Timeout { get; set; } = 25000;

    public HttpRequestMessage GetRequestMessage()
    {
        var request = new HttpRequestMessage(Method, RequestUrl);
        request.Headers.UserAgent.Clear();
        request.Content = Content;
        foreach(var key in Headers)
        {
            var isContentHeaders = key.ToString().ToLower().Contains("content");
            if (isContentHeaders && request.Content != null)
            {
                request.Content.Headers.TryAddWithoutValidation(key.Value, key.Value);
            }
            else
            {
                if(request.Content is null && isContentHeaders) continue;
                request.Headers.TryAddWithoutValidation(key.Key,key.Value);
            }
        }
        if(request.Headers.UserAgent.Count == 0) request.Headers.TryAddWithoutValidation("User-Agent", "PCL2-CE");
        return request;
    }
}
