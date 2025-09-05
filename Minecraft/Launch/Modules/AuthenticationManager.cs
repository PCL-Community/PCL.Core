using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PCL.Core.Minecraft.Launch.Services;
using PCL.Core.Minecraft.Launch.Services.Authentication;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Modules;

/// <summary>
/// 认证管理器 - 统一管理各种认证方式
/// </summary>
public static class AuthenticationManager {
    private static readonly Dictionary<McLoginType, Func<IAuthenticationProvider>> _providerFactories =
        new Dictionary<McLoginType, Func<IAuthenticationProvider>>();

    static AuthenticationManager() {
        // 注册认证提供者工厂
        RegisterProviderFactory(McLoginType.Ms, () => new MicrosoftAuthProvider());
        RegisterProviderFactory(McLoginType.Legacy, () => CreateLegacyProvider());
        RegisterProviderFactory(McLoginType.Auth, () => CreateAuthlibProvider());
    }

    /// <summary>
    /// 注册认证提供者工厂
    /// </summary>
    public static void RegisterProviderFactory(McLoginType type, Func<IAuthenticationProvider> factory) {
        _providerFactories[type] = factory;
    }

    /// <summary>
    /// 执行认证
    /// </summary>
    public static async Task<Result<LoginResult>> AuthenticateAsync() {
        try {
            var loginType = GetCurrentLoginType();
            var provider = CreateProvider(loginType);

            if (provider == null)
                return Result<LoginResult>.Failed($"不支持的登录类型: {loginType}");

            return await provider.AuthenticateAsync();
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"认证失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 验证现有令牌
    /// </summary>
    public static async Task<Result<LoginResult>> ValidateAsync(McLoginType type, string accessToken, string clientToken = null) {
        try {
            var provider = CreateProvider(type);
            if (provider == null)
                return Result<LoginResult>.Failed($"不支持的登录类型: {type}");

            return await provider.ValidateAsync(accessToken, clientToken);
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"令牌验证失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public static async Task<Result<LoginResult>> RefreshAsync(McLoginType type, string refreshToken) {
        try {
            var provider = CreateProvider(type);
            if (provider == null)
                return Result<LoginResult>.Failed($"不支持的登录类型: {type}");

            return await provider.RefreshAsync(refreshToken);
        } catch (Exception ex) {
            return Result<LoginResult>.Failed($"令牌刷新失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建认证提供者
    /// </summary>
    public static IAuthenticationProvider CreateProvider(McLoginType type) {
        if (_providerFactories.TryGetValue(type, out var factory)) {
            return factory();
        }

        return null;
    }

    /// <summary>
    /// 获取当前登录类型
    /// </summary>
    private static McLoginType GetCurrentLoginType() {
        // 这里应该从当前选择的档案中获取登录类型
        // 简化实现
        if (SelectedProfile != null)
            return SelectedProfile.Type;

        return McLoginType.Legacy; // 默认离线
    }

    /// <summary>
    /// 创建离线认证提供者
    /// </summary>
    private static IAuthenticationProvider CreateLegacyProvider() {
        if (SelectedProfile?.Type == McLoginType.Legacy) {
            return new LegacyAuthProvider(
                SelectedProfile.Username,
                SelectedProfile.Uuid,
                SelectedProfile.SkinType,
                SelectedProfile.SkinName
                );
        }

        // 默认离线用户
        return new LegacyAuthProvider("Player", null, 0, null);
    }

    /// <summary>
    /// 创建Authlib-Injector认证提供者
    /// </summary>
    private static IAuthenticationProvider CreateAuthlibProvider() {
        if (SelectedProfile?.Type == McLoginType.Auth) {
            return new AuthlibInjectorProvider(
                SelectedProfile.Server,
                SelectedProfile.Name,
                SelectedProfile.Password,
                SelectedProfile.ServerName ?? "第三方验证"
                );
        }

        throw new InvalidOperationException("未配置第三方验证信息");
    }

    /// <summary>
    /// 检查登录要求是否满足
    /// </summary>
    public static Result ValidateLoginRequirement(McInstance instance, McProfile profile) {
        var loginRequirement = Setup.Get("VersionServerLoginRequire", instance);

        switch (loginRequirement) {
            case 1: // 要求正版验证
                if (profile.Type != McLoginType.Ms)
                    return Result.Failed("当前实例要求使用正版验证");
                break;

            case 2: // 要求第三方验证
                if (profile.Type != McLoginType.Auth)
                    return Result.Failed("当前实例要求使用第三方验证");

                var requiredServer = Setup.Get("VersionServerAuthServer", instance);
                if (profile.Server?.Replace("/authserver", "") != requiredServer)
                    return Result.Failed("第三方验证服务器不匹配");
                break;

            case 3: // 要求正版或第三方验证
                if (profile.Type == McLoginType.Legacy)
                    return Result.Failed("当前实例不允许使用离线验证");

                if (profile.Type == McLoginType.Auth) {
                    var requiredAuthServer = Setup.Get("VersionServerAuthServer", instance);
                    if (profile.Server?.Replace("/authserver", "") != requiredAuthServer)
                        return Result.Failed("第三方验证服务器不匹配");
                }
                break;
        }

        return Result.Success();
    }

    // 这些是从原VB代码中需要引用的属性的占位符
    private static McProfile SelectedProfile => throw new NotImplementedException("需要从原代码引用");
    private static object Setup => throw new NotImplementedException("需要从原代码引用");
}
