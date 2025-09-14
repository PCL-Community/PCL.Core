using System;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Services.Authentication;

/*/// <summary>
/// 认证提供者接口
/// </summary>
public interface IAuthenticationProvider {
    /// <summary>
    /// 认证类型
    /// </summary>
    McLoginType Type { get; }

    /// <summary>
    /// 执行认证
    /// </summary>
    Task<Result<LoginResult>> AuthenticateAsync();

    /// <summary>
    /// 验证现有令牌
    /// </summary>
    Task<Result<LoginResult>> ValidateAsync(string accessToken, string clientToken = null);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<Result<LoginResult>> RefreshAsync(string refreshToken);
}

/// <summary>
/// 认证提供者基类
/// </summary>
public abstract class AuthenticationProviderBase : IAuthenticationProvider {
    public abstract McLoginType Type { get; }

    public abstract Task<Result<LoginResult>> AuthenticateAsync();

    public virtual Task<Result<LoginResult>> ValidateAsync(string accessToken, string clientToken = null) {
        if (string.IsNullOrEmpty(accessToken))
            return Task.FromResult(Result<LoginResult>.Failed("AccessToken不能为空"));

        return DoValidateAsync(accessToken, clientToken);
    }

    public virtual Task<Result<LoginResult>> RefreshAsync(string refreshToken) {
        if (string.IsNullOrEmpty(refreshToken))
            return Task.FromResult(Result<LoginResult>.Failed("RefreshToken不能为空"));

        return DoRefreshAsync(refreshToken);
    }

    protected abstract Task<Result<LoginResult>> DoValidateAsync(string accessToken, string clientToken);
    protected abstract Task<Result<LoginResult>> DoRefreshAsync(string refreshToken);

    protected void LogAuth(string message) {
        Log($"[Auth-{Type}] {message}");
    }

    // 这些是从原VB代码中需要引用的方法的占位符
    protected static void Log(string message) {
        throw new NotImplementedException("需要从原代码引用Log方法");
    }
}*/
