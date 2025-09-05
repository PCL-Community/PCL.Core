using System.Threading.Tasks;
using PCL.Core.Minecraft.McLaunch.Modules;
using AuthenticationManager = System.Net.AuthenticationManager;

namespace PCL.Core.Minecraft.McLaunch;

public static class LaunchOrchestrator
{
    public static async Task<Result<>> LaunchMinecraftAsync(LaunchOptions options)
    {
        // 1. 预检查
        var preCheckResult = await PreCheckService.ValidateAsync(options);
        if (!preCheckResult.IsSuccess)
            return Result<>.Failed(preCheckResult.Error);
        
        // 2. 认证
        var authResult = await AuthenticationManager.AuthenticateAsync();
        if (!authResult.IsSuccess)
            return Result<>.Failed(authResult.Error);
        
        // 3. Java检查
        var javaResult = await JavaManager.SelectJavaAsync();
        if (!javaResult.IsSuccess)
            return Result<>.Failed(javaResult.Error);
        
        // 4. 构建参数
        var argumentsResult = ArgumentBuilder.BuildArguments(options, authResult.Value, javaResult.Value);
        if (!argumentsResult.IsSuccess)
            return Result<>.Failed(argumentsResult.Error);
        
        // 5. 启动进程
        return await ProcessLauncher.StartMinecraftAsync(argumentsResult.Value);
    }
}

