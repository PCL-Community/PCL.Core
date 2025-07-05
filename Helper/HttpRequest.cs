using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;
using PCL.Core.Utils.Net;

namespace PCL.Core.Helper;

public static class HttpRequest
{

    public static async Task<HttpResponseMessage> GetServerResponse(HttpRequestOptions options)
    {
        Exception? lastException = null;
        while (options.Retry >0)
        {
            try
            {
                using (HttpRequestMessage request = options.GetRequestMessage())
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(options.Timeout))
                    {
                        return await HttpClientManager.GetClient()
                            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    }
                }
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
}