using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;
namespace PCL.Core.Utils;

public class Network
{
    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None
    };
    private static readonly HttpClient Client = new(Handler);
    
    public static async Task<HttpResponseMessage> GetResponse(
        string requestUrl,
        HttpMethod method,
        Dictionary<string,string>? headers = null,
        object? data = null,
        int timeout = 25000
        )
    {
        int retry = 3;
        retry:
        using (HttpRequestMessage request = new(method,requestUrl))
        {
            if (data is not null) 
                request.Content = new ByteArrayContent((data is string)? (data as string).GetBytes():data as byte[]);
            if (headers is not null) foreach (var header in headers)
            {
                if (header.Key.StartsWith("Content", StringComparison.OrdinalIgnoreCase) && request.Content is not null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    continue;
                }

                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using (CancellationTokenSource cts = new(timeout))
            {
                HttpResponseMessage response =
                    await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                try
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    if (retry > 0)
                    {
                        retry--;
                        goto retry;
                    }
                    // 尝试读取远程服务器返回的数据数据
                    try
                    {
                        if (response.Content.Headers.ContentLength <= 1024 * 1024)
                            ex.Data["ServerResponse"] = (await response.Content.ReadAsByteArrayAsync()).GetString();
                    }
                    catch
                    {
                        
                    }
                    throw;
                }
            }
        }
    }
}