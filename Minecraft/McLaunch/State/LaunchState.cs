namespace PCL.Core.Minecraft.McLaunch.State;

public enum LaunchState
{
    Idle,
    Validating,
    Authenticating,
    BuildingArguments,
    PreLaunching,
    Launching,
    WaitingForWindow,
    Finished,
    Failed
}

