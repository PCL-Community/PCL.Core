using System;
using PCL.Core.App.Arguments;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

[ArgumentHandler]
public class GpuHandler(): GeneralHandler("gpu")
{
    public override HandleResult Handle(string[] args)
    {
        if (args is not ["--gpu", var mode]) return HandleResult.NotHandled;
        try
        {
            ProcessInterop.SetGpuPreference(mode.Trim('\"'));
            return HandleResult.HandledAndExit;
        }
        catch (Exception e)
        {
            ParentContext.Fatal("调整显卡设置时发生错误", e);
            return HandleResult.HandledAndExit;
        }
    }
}