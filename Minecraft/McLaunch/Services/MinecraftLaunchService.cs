using System;

namespace PCL.Core.Minecraft.McLaunch.Services;

public class MinecraftLaunchService {
    private static readonly Lazy<MinecraftLaunchService> _instance =
        new(() => new MinecraftLaunchService());

    public static MinecraftLaunchService Instance => _instance.Value;

    private readonly IAuthenticationService _authService;
    private readonly IJavaManagementService _javaService;
}


