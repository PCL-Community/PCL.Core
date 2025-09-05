using System;
using PCL.Core.Minecraft.Launch.State;

namespace PCL.Core.Minecraft.Launch;

public static class LaunchEventManager
{
    public static event Action<LaunchState> StateChanged;
    public static event Action<string> LogReceived;
    public static event Action<Exception> ErrorOccurred;
    
    public static void RaiseStateChanged(LaunchState newState)
    {
        StateChanged?.Invoke(newState);
    }
    
    public static void RaiseLogReceived(string message)
    {
        LogReceived?.Invoke(message);
    }
    
    public static void RaiseError(Exception error)
    {
        ErrorOccurred?.Invoke(error);
    }
}

