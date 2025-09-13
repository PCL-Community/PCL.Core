/*
using System;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Services.Authentication;

/// <summary>
/// Authlib-Injector第三方认证提供者
/// </summary>
public class AuthlibInjectorProvider : AuthenticationProviderBase {
    public override McLoginType Type => McLoginType.Auth;

    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly string _description;

    public AuthlibInjectorProvider(string baseUrl, string username, string password, string description = "第三方验证") {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _description = description;
    }

    public override async Task<Result<LoginResult>> AuthenticateAsync() {
        try {
            LogAuth($"开始第三方验证 ({_description})");

            // 执行登录请求
            var loginResult = await LoginAsync();
            if (!loginResult.IsSuccess)
                return loginResult;

            LogAuth("第三方验证成功");
            return loginResult;
        } catch (Exception ex) {
            LogAuth($"第三方验证失败: {ex.Message}");
            return Result<LoginResult>.Failed($"第三方验证失败: {ex.Message}", ex);
        }
    }

    protected override async Task<Result<LoginResult>> DoValidateAsync(string accessToken, string clientToken) {
        try {
            LogAuth("验证第三方令牌");

            var requestData = new JObject(
                new JProperty("accessToken", accessToken),
                new JProperty("clientToken", clientToken)
                );

            var response = await NetRequestAsync($"{_baseUrl}/validate", "POST", requestData.ToString(0));

            // validate接口通常没有返回值，成功则不抛出异常
            LogAuth("令牌验证成功");

            // 返回基本信息（需要从存储或其他地方获取详细信息）
            return Result<LoginResult>.Success(new LoginResult {
                AccessToken = accessToken,
                ClientToken = clientToken,
                Type = "Auth"
            });
        } catch (Exception ex) {
            LogAuth($"令牌验证失败: {ex.Message}");
            return Result<LoginResult>.Failed($"令牌验证失败: {ex.Message}", ex);
        }
    }

    protected override async Task<Result<LoginResult>> DoRefreshAsync(string refreshToken) {
        try {
            LogAuth("刷新第三方令牌");

            // Authlib-Injector使用accessToken作为refreshToken
            var refreshData = new JObject(
                new JProperty("accessToken", refreshToken),
                new JProperty("requestUser", true)
                );

            var response = await NetRequestAsync($"{_baseUrl}/refresh", "POST", refreshData.ToString(0));
            var loginJson = JObject.Parse(response);

            if (loginJson["selectedProfile"] == null)
                return Result<LoginResult>.Failed("刷新后未找到选定的角色");

            var result = new LoginResult {
                AccessToken = loginJson["accessToken"]?.ToString(),
                ClientToken = loginJson["clientToken"]?.ToString(),
                Uuid = loginJson["selectedProfile"]?["id"]?.ToString(),
                Name = loginJson["selectedProfile"]?["name"]?.ToString(),
                Type = "Auth"
            };

            LogAuth("令牌刷新成功");
            return Result<LoginResult>.Success(result);
        } catch (Exception ex) {
            LogAuth($"令牌刷新失败: {ex.Message}");
            return Result<LoginResult>.Failed($"令牌刷新失败: {ex.Message}", ex);
        }
    }

    private async Task<Result<LoginResult>> LoginAsync() {
        try {
            var requestData = new JObject(
                new JProperty("agent", new JObject(
                    new JProperty("name", "Minecraft"),
                    new JProperty("version", 1)
                    )),
                new JProperty("username", _username),
                new JProperty("password", _password),
                new JProperty("requestUser", true)
                );

            var response = await NetRequestAsync($"{_baseUrl}/authenticate", "POST", requestData.ToString(0));
            var loginJson = JObject.Parse(response);

            // 检查是否有可用角色
            if (loginJson["availableProfiles"]?.Count() == 0)
                return Result<LoginResult>.Failed("该账户没有创建角色，请先创建角色后再试！");

            // 选择角色
            var (selectedName, selectedId) = await SelectProfileAsync(loginJson);
            if (string.IsNullOrEmpty(selectedName))
                return Result<LoginResult>.Failed("未选择有效的角色");

            // 获取服务器信息
            var serverName = await GetServerNameAsync();

            var result = new LoginResult {
                AccessToken = loginJson["accessToken"]?.ToString(),
                ClientToken = loginJson["clientToken"]?.ToString(),
                Name = selectedName,
                Uuid = selectedId,
                Type = "Auth"
            };

            return Result<LoginResult>.Success(result);
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"登录请求失败: {ex.Message}", ex);
        }
    }

    private async Task<(string name, string id)> SelectProfileAsync(JObject loginJson) {
        // 如果已经有选定的角色，直接使用
        if (loginJson["selectedProfile"] != null) {
            return (
                loginJson["selectedProfile"]["name"]?.ToString(),
                loginJson["selectedProfile"]["id"]?.ToString()
            );
        }

        // 如果只有一个角色，直接选择
        if (loginJson["availableProfiles"]?.Count() == 1) {
            var profile = loginJson["availableProfiles"][0];
            return (profile["name"]?.ToString(), profile["id"]?.ToString());
        }

        // 多个角色时，需要用户选择（这里简化为选择第一个）
        // 实际实现中应该弹出选择对话框
        var firstProfile = loginJson["availableProfiles"]?[0];
        return (firstProfile?["name"]?.ToString(), firstProfile?["id"]?.ToString());
    }

    private async Task<string> GetServerNameAsync() {
        try {
            var serverUrl = _baseUrl.Replace("/authserver", "");
            var response = await NetRequestAsync(serverUrl, "GET");
            var serverJson = JObject.Parse(response);
            return serverJson["meta"]?["serverName"]?.ToString() ?? "Unknown Server";
        } catch (Exception ex) {
            LogAuth($"获取服务器名称失败: {ex.Message}");
            return "Unknown Server";
        }
    }

    private async Task<string> NetRequestAsync(string url, string method, string data = null) {
        // 这里应该调用原VB代码中的NetRequestRetry方法
        // 简化实现
        throw new NotImplementedException("需要实现网络请求逻辑");
    }
}
*/
