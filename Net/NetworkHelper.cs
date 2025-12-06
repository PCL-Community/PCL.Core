using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using PCL.Core.Logging;

namespace PCL.Core.Net;

public static class NetworkHelper
{
    public static int NewTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
    
    /// <summary>
    /// 测试 HTTP 连通性
    /// </summary>
    /// <param name="url">目标 Url，留空则为 baidu.com</param>
    /// <param name="timeout">超时时间 (默认 3000 ms)</param>
    /// <returns>若 HTTP 连接成功，则返回 true；反之，则返回 false</returns>
    public static async Task<bool> TestHttpConnectionAsync(string url = "http://www.baidu.com", int timeout = 3000)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
                
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = await httpClient.SendAsync(request);
                
                return response.IsSuccessStatusCode;
            }
        }
        catch (HttpRequestException ex)
        {
            LogWrapper.Error($"HTTP 请求异常: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            LogWrapper.Error("请求超时");
            return false;
        }
        catch
        {
            return false;
        }
    }
}
