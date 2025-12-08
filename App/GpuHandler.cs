using System;
using PCL.Core.App.Arguments;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

[ArgumentHandler]
public class GpuHandler(): GeneralHandler("gpu")
{
    public override HandleResult Handle(string[] args)
    {
        if (args is not ["--gpu", var mode]) return new HandleResult(HandleResultType.NotHandled);
        try
        {
            ProcessInterop.SetGpuPreference(mode.Trim('\"'));
            ParentContext.Info($"已将显卡设置调整为 {mode}");
            return new HandleResult(HandleResultType.HandledAndExit);
        }
        catch (Exception e)
        {
            ParentContext.Fatal("调整显卡设置时发生错误", e);
            return new HandleResult(HandleResultType.HandledAndExit, (int)ProcessExitCode.Failed);
        }
    }
}