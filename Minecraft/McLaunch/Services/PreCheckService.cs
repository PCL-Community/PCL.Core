using PCL.Core.Minecraft.McLaunch.State;

namespace PCL.Core.Minecraft.McLaunch.Services;

using System;
using System.Threading.Tasks;

/// <summary>
/// 启动前预检查服务
/// </summary>
public static class PreCheckService
{
    /// <summary>
    /// 验证启动配置和环境
    /// </summary>
    public static async Task<Result> ValidateAsync(LaunchOptions options)
    {
        try
        {
            // 检查路径
            var pathResult = ValidatePaths(options);
            if (!pathResult.IsSuccess)
                return pathResult;

            // 检查实例状态
            var instanceResult = ValidateInstance(options);
            if (!instanceResult.IsSuccess)
                return instanceResult;

            // 检查档案有效性
            var profileResult = ValidateProfile();
            if (!profileResult.IsSuccess)
                return profileResult;

            // 检查登录要求
            var loginResult = ValidateLoginRequirements(options);
            if (!loginResult.IsSuccess)
                return loginResult;

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failed($"预检查失败: {ex.Message}", ex);
        }
    }

    private static Result ValidatePaths(LaunchOptions options)
    {
        var instance = options.Version ?? McInstanceCurrent;
        if (instance == null)
            return Result.Failed("未选择Minecraft实例");

        // 检查路径中的特殊字符
        if (instance.PathIndie.Contains("!") || instance.PathIndie.Contains(";"))
            return Result.Failed($"游戏路径中不可包含 ! 或 ;（{instance.PathIndie}）");

        if (instance.Path.Contains("!") || instance.Path.Contains(";"))
            return Result.Failed($"游戏路径中不可包含 ! 或 ;（{instance.Path}）");

        // UTF-8代码页下的路径检查
        if (IsUtf8CodePage() && !Setup.Get("HintDisableGamePathCheckTip") && !instance.Path.IsASCII())
        {
            // 这里应该弹出确认对话框，简化为警告
            Log($"[PreCheck] 路径包含非ASCII字符，可能影响游戏运行: {instance.Path}");
        }

        return Result.Success();
    }

    private static Result ValidateInstance(McLaunchOptions options)
    {
        var instance = options.Version ?? McInstanceCurrent;
        if (instance == null)
            return Result.Failed("未选择Minecraft实例");

        try
        {
            instance.Load();
            if (instance.State == McInstanceState.Error)
                return Result.Failed($"Minecraft存在问题：{instance.Info}");
        }
        catch (Exception ex)
        {
            return Result.Failed($"加载实例失败：{ex.Message}", ex);
        }

        return Result.Success();
    }

    private static Result ValidateProfile()
    {
        if (SelectedProfile == null)
            return Result.Failed("请先选择一个档案再启动游戏！");

        // 这里应该调用原来的IsProfileVaild()方法
        // 简化实现
        var checkResult = IsProfileVaild();
        if (!string.IsNullOrEmpty(checkResult))
            return Result.Failed(checkResult);

        return Result.Success();
    }

    private static Result ValidateLoginRequirements(LaunchOptions options)
    {
        var instance = options.Version ?? McInstanceCurrent;
        
        // 检查是否要求正版验证
        if (instance.Version.HasLabyMod || Setup.Get("VersionServerLoginRequire", instance) == 1)
        {
            if (SelectedProfile.Type != McLoginType.Ms)
                return Result.Failed("当前实例要求使用正版验证，请使用正版验证档案启动游戏！");
        }

        // 检查是否要求第三方验证
        if (Setup.Get("VersionServerLoginRequire", instance) == 2)
        {
            if (SelectedProfile.Type != McLoginType.Auth)
                return Result.Failed("当前实例要求使用第三方验证，请使用第三方验证档案启动游戏！");

            var requiredServer = Setup.Get("VersionServerAuthServer", instance);
            if (SelectedProfile.Server.BeforeLast("/authserver") != requiredServer)
                return Result.Failed("当前档案使用的第三方验证服务器与实例要求使用的不一致！");
        }

        // 检查是否要求正版或第三方验证
        if (Setup.Get("VersionServerLoginRequire", instance) == 3)
        {
            if (SelectedProfile.Type == McLoginType.Legacy)
                return Result.Failed("当前实例要求使用正版验证或第三方验证，请使用符合要求的档案启动游戏！");

            if (SelectedProfile.Type == McLoginType.Auth)
            {
                var requiredServer = Setup.Get("VersionServerAuthServer", instance);
                if (SelectedProfile.Server.BeforeLast("/authserver") != requiredServer)
                    return Result.Failed("当前档案使用的第三方验证服务器与实例要求使用的不一致！");
            }
        }

        return Result.Success();
    }

    // 这些是从原VB代码中需要引用的方法和属性的占位符
    // 实际实现中需要正确引用
    private static McInstance.McInstance McInstanceCurrent => throw new NotImplementedException("需要从原代码引用");
    private static McProfile SelectedProfile => throw new NotImplementedException("需要从原代码引用");
    private static bool IsUtf8CodePage() => throw new NotImplementedException("需要从原代码引用");
    private static object Setup => throw new NotImplementedException("需要从原代码引用");
    private static string IsProfileVaild() => throw new NotImplementedException("需要从原代码引用");
    private static void Log(string message) => throw new NotImplementedException("需要从原代码引用");
}
