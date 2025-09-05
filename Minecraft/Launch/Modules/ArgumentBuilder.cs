using System;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch.Modules;

public static class ArgumentBuilder
{
    public static Result<string> BuildArguments(LaunchOptions options, LoginResult loginResult, JavaInfo selectedJavaInfo)
    {
        try
        {
            var builder = new LaunchArgumentBuilder(options.Version, selectedJavaInfo, loginResult);
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