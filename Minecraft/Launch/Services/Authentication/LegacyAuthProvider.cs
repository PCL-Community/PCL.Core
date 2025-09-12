/*
using System.Threading.Tasks;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Services.Authentication;

/// <summary>
/// 微软正版认证提供者
/// </summary>
public class LegacyAuthProvider : AuthenticationProviderBase {
    public override McLoginType Type => McLoginType.Ms;

    public override async Task<Result<LoginResult>> AuthenticateAsync() {
        try {
            LogAuth("开始正版验证流程");

            // Step 1: 获取OAuth令牌
            var oauthResult = await GetOAuthTokensAsync();
            if (!oauthResult.IsSuccess)
                return Result<LoginResult>.Failed(oauthResult.Error);

            var (accessToken, refreshToken) = oauthResult.Value;

            // Step 2: 获取XBL令牌
            var xblResult = await GetXBLTokenAsync(accessToken);
            if (!xblResult.IsSuccess)
                return Result<LoginResult>.Failed(xblResult.Error);

            // Step 3: 获取XSTS令牌
            var xstsResult = await GetXSTSTokenAsync(xblResult.Value);
            if (!xstsResult.IsSuccess)
                return Result<LoginResult>.Failed(xstsResult.Error);

            var (xstsToken, uhs) = xstsResult.Value;

            // Step 4: 获取Minecraft访问令牌
            var mcTokenResult = await GetMinecraftAccessTokenAsync(xstsToken, uhs);
            if (!mcTokenResult.IsSuccess)
                return Result<LoginResult>.Failed(mcTokenResult.Error);

            // Step 5: 验证账户是否拥有Minecraft
            var ownershipResult = await VerifyMinecraftOwnershipAsync(mcTokenResult.Value);
            if (!ownershipResult.IsSuccess)
                return Result<LoginResult>.Failed(ownershipResult.Error);

            // Step 6: 获取玩家档案信息
            var profileResult = await GetPlayerProfileAsync(mcTokenResult.Value);
            if (!profileResult.IsSuccess)
                return Result<LoginResult>.Failed(profileResult.Error);

            var (uuid, username, profileJson) = profileResult.Value;

            LogAuth("正版验证完成");

            return Result<LoginResult>.Success(new LoginResult {
                Name = username,
                Uuid = uuid,
                AccessToken = mcTokenResult.Value,
                Type = "Microsoft",
                ClientToken = uuid,
                ProfileJson = profileJson
            });
        } catch (Exception ex) {
            LogAuth($"正版验证失败: {ex.Message}");
            return Result<LoginResult>.Failed($"正版验证失败: {ex.Message}", ex);
        }
    }

    protected override async Task<Result<LoginResult>> DoValidateAsync(string accessToken, string clientToken) {
        try {
            LogAuth("验证现有正版令牌");

            // 尝试使用现有令牌获取玩家档案
            var profileResult = await GetPlayerProfileAsync(accessToken);
            if (!profileResult.IsSuccess)
                return Result<LoginResult>.Failed("令牌验证失败");

            var (uuid, username, profileJson) = profileResult.Value;

            return Result<LoginResult>.Success(new LoginResult {
                Name = username,
                Uuid = uuid,
                AccessToken = accessToken,
                Type = "Microsoft",
                ClientToken = clientToken ?? uuid,
                ProfileJson = profileJson
            });
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"令牌验证失败: {ex.Message}", ex);
        }
    }

    protected override async Task<Result<LoginResult>> DoRefreshAsync(string refreshToken) {
        try {
            LogAuth("刷新正版令牌");

            // 使用RefreshToken获取新的AccessToken
            var refreshResult = await RefreshOAuthTokenAsync(refreshToken);
            if (!refreshResult.IsSuccess)
                return Result<LoginResult>.Failed(refreshResult.Error);

            var (newAccessToken, newRefreshToken) = refreshResult.Value;

            // 重新执行认证流程的后续步骤
            return await AuthenticateWithAccessTokenAsync(newAccessToken);
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"令牌刷新失败: {ex.Message}", ex);
        }
    }

    private async Task<Result<(string accessToken, string refreshToken)>> GetOAuthTokensAsync() {
        // 这里应该实现设备代码流程或其他OAuth流程
        // 调用原VB代码中的MsLoginStep1New或MsLoginStep1Refresh方法
        throw new NotImplementedException("需要实现OAuth令牌获取逻辑");
    }

    private async Task<Result<string>> GetXBLTokenAsync(string accessToken) {
        // 实现XBL令牌获取
        // 调用原VB代码中的MsLoginStep2方法
        throw new NotImplementedException("需要实现XBL令牌获取逻辑");
    }

    private async Task<Result<(string xstsToken, string uhs)>> GetXSTSTokenAsync(string xblToken) {
        // 实现XSTS令牌获取
        // 调用原VB代码中的MsLoginStep3方法
        throw new NotImplementedException("需要实现XSTS令牌获取逻辑");
    }

    private async Task<Result<string>> GetMinecraftAccessTokenAsync(string xstsToken, string uhs) {
        // 实现Minecraft访问令牌获取
        // 调用原VB代码中的MsLoginStep4方法
        throw new NotImplementedException("需要实现Minecraft令牌获取逻辑");
    }

    private async Task<Result> VerifyMinecraftOwnershipAsync(string accessToken) {
        // 实现Minecraft所有权验证
        // 调用原VB代码中的MsLoginStep5方法
        throw new NotImplementedException("需要实现Minecraft所有权验证逻辑");
    }

    private async Task<Result<(string uuid, string username, string profileJson)>> GetPlayerProfileAsync(string accessToken) {
        // 实现玩家档案获取
        // 调用原VB代码中的MsLoginStep6方法
        throw new NotImplementedException("需要实现玩家档案获取逻辑");
    }

    private async Task<Result<(string accessToken, string refreshToken)>> RefreshOAuthTokenAsync(string refreshToken) {
        // 实现令牌刷新
        // 调用原VB代码中的MsLoginStep1Refresh方法
        throw new NotImplementedException("需要实现令牌刷新逻辑");
    }

    private async Task<Result<LoginResult>> AuthenticateWithAccessTokenAsync(string accessToken) {
        // 使用新的AccessToken重新执行认证流程
        throw new NotImplementedException("需要实现基于AccessToken的认证逻辑");
    }
}
*/
