using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using PCL.Core.Logging;
using PCL.Core.Net;
using PCL.Core.Utils.Exts;

namespace PCL.Core.Minecraft.Accounts.Utils;

public class OAuthClient:IDisposable
{
    private string _client;

    private string? _deviceEndpoint;

    private string _tokenEndpoint;

    private string? _authorizeEndpoint;

    public event Action<OAuthResult?> AuthorizeCallback;

    public event Action<OAuthDeviceCallback> DeviceCallback;

    private CancellationTokenSource cts = new CancellationTokenSource();

    private RoutedWebServer _server = new();

    public OAuthClient(
        string clientId,
        string? deviceEndpoint,
        string tokenEndpoint,
        string? authorizeEndpoint
        )
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(deviceEndpoint ?? authorizeEndpoint);
        ArgumentException.ThrowIfNullOrEmpty(tokenEndpoint);
        _tokenEndpoint = tokenEndpoint;
        _deviceEndpoint = deviceEndpoint;
        _authorizeEndpoint = authorizeEndpoint;
        _client = clientId;
    }

    private async Task _LoopValidate(string device,int interval,string[] scopes)
    {
        var allowRetry = 5;
        OAuthResult? result = null;
        while (allowRetry >= 0)
        {
            cts.Token.ThrowIfCancellationRequested();
            var requestData =
                Uri.EscapeDataString($"client_id={_client}&device_code={device}&scopes={string.Join(" ", scopes)}");
            await Task.Delay(TimeSpan.FromSeconds(interval));
            using var response = await HttpRequestBuilder.Create(_tokenEndpoint, HttpMethod.Post)
                .WithContent(new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded"))
                .WithHeader("Accept", "application/json").SendAsync();
            result = await response.AsJsonAsync<OAuthResult>();
            if(!result!.Succeed)
                switch (result.Error)
                {
                    case "authorization_pending":
                    case "slow_down":
                        continue;
                    case "expired":
                        AuthorizeCallback?.Invoke(result);
                        return;
                    default:
                        LogWrapper.Error("Account",$"OAuth 登录失败，错误信息: {result?.Description}，剩余重试次数 {allowRetry}");
                        allowRetry--;
                        break;
                }
            AuthorizeCallback?.Invoke(result);
            
            
        }

        AuthorizeCallback?.Invoke(result);
    }

    private async Task _AuthorizeByDeviceFlow(string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(DeviceCallback);
        if (_deviceEndpoint.IsNullOrWhiteSpace()) 
            throw new InvalidOperationException("Not allow to call this method while DeviceEndpoint is null");
        var requestData = Uri.EscapeDataString($"client_id{_client}&=scope={string.Join(" ", scopes)}");
        using var response = await HttpRequestBuilder.Create(_deviceEndpoint, HttpMethod.Post)
            .WithTimeOut(25000)
            .WithContent(new StringContent(requestData,Encoding.UTF8,"application/x-www-form-urlencoded"))
            .WithHeader("Accept","application/json")
            .SendAsync(true);
        var jsonString = await response.AsJsonAsync<JsonNode>();
        if (jsonString?["error"] is not null) 
            throw new VerificationException(jsonString!["error_description"]!.ToString());
        var deviceCode = jsonString!["device_code"]!.ToString();
        var interval = jsonString!["interval"]!.GetValue<int>();
        var callbackValue = jsonString?.GetValue<OAuthDeviceCallback>();
        Task.Run(() => _LoopValidate(deviceCode, interval, scopes));
    }
    
    public void AuthorizeAsync()
    {
        if (_authorizeEndpoint.IsNullOrWhiteSpace()) 
            throw new InvalidOperationException("Not allow to call this method while DeviceEndpoint is null");
        _server.
        var url = $"{_authorizeEndpoint}";
        using var process = new Process();
        process.StartInfo.Arguments;
    }
    

    public void Dispose()
    {
        cts.Cancel();
        // TODO 在此释放托管资源
    }

}
