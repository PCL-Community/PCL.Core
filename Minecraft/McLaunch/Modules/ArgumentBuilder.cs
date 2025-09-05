using System;
using PCL.Core.Minecraft.McLaunch.State;

namespace PCL.Core.Minecraft.McLaunch.Modules;

public static class ArgumentBuilder
{
    public static Result<string> BuildArguments(LaunchOptions options, LoginResult loginResult, Java selectedJava)
    {
        try
        {
            var builder = new LaunchArgumentBuilder(options.Version, selectedJava, loginResult);
            var arguments = builder
                .AddJvmArguments()
                .AddGameArguments()
                .Build();
            
            return Result<string>.Success(arguments);
        }
        catch (Exception ex)
        {
            return Result<string>.Failed(ex.Message);
        }
    }
}