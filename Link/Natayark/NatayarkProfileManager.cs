using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

using PCL.Core.Utils.OS;
using PCL.Core.Net;
using PCL.Core.ProgramSetup;
using PCL.Core.App;
using PCL.Core.Logging;

namespace PCL.Core.Link.Natayark
{
    public class NatayarkProfileManager
    {
        public class NaidUser
        {
            public Int32 Id { get; set; }
            public string? Email { get; set; }
            public string? Username { get; set; }
            public string? AccessToken { get; set; }
            public string? RefreshToken { get; set; }
            /// <summary>
            /// Natayark ID 状态，1 为正常
            /// </summary>
            public int Status { get; set; }
            public bool IsRealname { get; set; }
            public string? LastIp { get; set; }

        }

        public static NaidUser NaidProfile = new();
        public static Exception? Exception { get; set; }

        private bool _isGettingData = false;
        public void GetNaidData(string token, bool isRefresh = false, bool isRetry = false)
        {
            Basics.RunInNewThread(() => GetNaidDataSync(token, isRefresh, isRetry));
        }
        public bool GetNaidDataSync(string token, bool isRefresh = false, bool isRetry = false)
        {
            if (_isGettingData) { return false; }
            _isGettingData = true;
            try
            {
                // 获取 AccessToken 和 RefreshToken
                string requestData = $"grant_type={(isRefresh, "refresh_token", "authorization_code")}&client_id={EnvironmentInterop.GetSecret("NatayarkClientId")}&client_secret={EnvironmentInterop.GetSecret("NatayarkClientSecret")}&{(isRefresh, "refresh_token", "code")}={token}&redirect_uri=http://localhost:29992/callback";
                Thread.Sleep(500);
                HttpContent httpContent = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");
                string? result = HttpRequestBuilder.Create("https://account.naids.com/api/oauth2/token", HttpMethod.Post)
                    .WithContent(httpContent).Build().Result.GetResponse().Content.ToString();
                if (result == null)
                {
                    throw new Exception("获取 AccessToken 与 RefreshToken 失败，返回内容为空");
                }
                JsonObject? data = (JsonObject?)JsonObject.Parse(result);
                if (data == null)
                {
                    throw new Exception("获取 AccessToken 与 RefreshToken 失败，解析返回内容失败");
                }
                NaidProfile.AccessToken = data["access_token"]?.ToString() ?? string.Empty;
                NaidProfile.RefreshToken = data["refresh_token"]?.ToString() ?? string.Empty;
                string? expiresAt = data["refresh_token_expires_at"]?.ToString();

                // 获取用户信息
                string? receivedUserData = HttpRequestBuilder.Create("https://account.naids.com/api/api/user/data", HttpMethod.Get)
                    .SetHeader("Authorization", $"Bearer {NaidProfile.AccessToken}").Build().Result.GetResponse().Content.ToString();
                if (receivedUserData == null)
                {
                    throw new Exception("获取 Natayark 用户信息失败，返回内容为空");
                }
                JsonObject? userData = (JsonObject?)JsonObject.Parse(receivedUserData)?["data"];
                if (userData == null)
                {
                    throw new Exception("获取 Natayark 用户信息失败，解析返回内容失败");
                }
                else
                {
                    NaidProfile.Id = userData["id"]?.GetValue<int>() ?? 0;
                    NaidProfile.Username = userData["username"]?.ToString() ?? string.Empty;
                    NaidProfile.Email = userData["email"]?.ToString() ?? string.Empty;
                    NaidProfile.Status = userData["status"]?.GetValue<int>() ?? 0;
                    NaidProfile.IsRealname = userData["realname"]?.GetValue<bool>() ?? false;
                    NaidProfile.LastIp = userData["last_ip"]?.ToString() ?? string.Empty;

                    // 保存数据
                    Setup.Link.NaidRefreshToken = NaidProfile.RefreshToken;
                    Setup.Link.NaidRefreshExpireTime = expiresAt;
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (isRetry)
                {
                    NaidProfile = new NaidUser();
                    Setup.Link.NaidRefreshToken = string.Empty;
                    throw new Exception("获取 Natayark 用户数据失败，请尝试前往设置重新登录", ex);
                }
                else
                {
                    if(ex.Message.Contains("invalid access token"))
                    {
                        LogWrapper.Warn("Link", "Naid Access Token 无效，尝试刷新登录");
                        return GetNaidDataSync(Setup.Link.NaidRefreshToken, true, true);
                    }
                    else if (ex.Message.Contains("invalid_grant"))
                    {
                        LogWrapper.Warn("Link", "Naid 验证代码无效，原始信息: " + ex.ToString());
                    }
                    else if (ex.Message.Contains("401"))
                    {
                        NaidProfile = new NaidUser();
                        Setup.Link.NaidRefreshToken = string.Empty;
                        throw new Exception("Natayark 账号信息已过期，请前往设置重新登录！", ex);
                    }
                    else
                    {
                        NaidProfile = new NaidUser();
                        Setup.Link.NaidRefreshToken = string.Empty;
                        throw new Exception("Naid 登录失败，请尝试前往设置重新登录");
                    }
                }
                return false;
            }
            finally
            {
                _isGettingData = false;
            }
        }
    }
}
