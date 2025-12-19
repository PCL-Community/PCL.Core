using System;
using PCL.Core.Utils.OS;

namespace PCL.Core.App.Arguments;

public static class GpuArgument
{
    private static LifecycleContext Context => ArgumentService.Context;
    
    [ArgumentHandler("GPU")]
    public static HandleResult HandleArgument(string[] args)
    {
        if (args is not ["--gpu", var mode]) return new HandleResult(HandleResultType.NotHandled);
        try
        {
            ProcessInterop.SetGpuPreference(mode.Trim('\"'));
            Context.Info($"已将显卡设置调整为 {mode}");
            return new HandleResult(HandleResultType.HandledAndExit);
        }
        catch (Exception e)
        {
            Context.Error("设置显卡偏好时发生错误", e);
            return new HandleResult(HandleResultType.HandledAndExit, (int)ProcessExitCode.Failed);
        }
    }
}