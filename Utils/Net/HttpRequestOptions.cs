using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PCL.Core.Utils;

public class HttpRequestOptions
{

    public string? RequestUrl
    {
        get
        {
            
            return _requestUrl ?? "";
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(RequestUrl, "不能为空或 Null");
            _requestUrl = value;
        }
    }

    public HttpMethod Method = HttpMethod.Get;

    public int Retry = 3;

    public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();

    public HttpContent? Content
    {
        get
        {
            return _content;
        }
        set
        {
            _content = value ?? throw new ArgumentNullException(RequestUrl, "请求载荷不能为 Null");
        }
    }
    private HttpContent? _content;
    
    private string? _requestUrl;

    public int Timeout = 25000;

    public HttpRequestMessage GetRequestMessage()
    {
        HttpRequestMessage request = new HttpRequestMessage(Method, RequestUrl);
        request.Headers.UserAgent.Clear();
        request.Content = Content;
        foreach(var key in Headers)
        {
            var isContentHeaders = key.ToString().ToLower().Contains("content");
            if (isContentHeaders && request.Content is not null)
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