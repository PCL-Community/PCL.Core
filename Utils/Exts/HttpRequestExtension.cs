using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace PCL.Core.Utils.Exts;

public static class HttpRequestExtension
{
    public static HttpRequestMessage Clone(this HttpRequestMessage request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content?.Clone(),
            Version = request.Version
        };

        // 复制请求头
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            clone.Options.TryAdd(option.Key, option.Value);

        return clone;
    }

    private static HttpContent Clone(this HttpContent content)
    {
        if (content == null)
            return null;

        // 创建内存流复制内容
        var ms = new MemoryStream();
        content.CopyToAsync(ms).Wait();
        ms.Position = 0;

        var clone = new StreamContent(ms);

        // 复制内容头
        foreach (var header in content.Headers)
            clone.Headers.Add(header.Key, header.Value);

        return clone;
    }
}