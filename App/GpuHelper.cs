using System;
using Accessibility;
using PCL.Core.App.Arguments;
using PCL.Core.Logging;

namespace PCL.Core.App;

public static class GpuHelper
{
    public static void SetGpuPreference(string executable, bool wantHighPerformance = true)
    {
        const string gpuPreferenceRegKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
        const string gpuPreferenceRegValueHigh = "GpuPreference=2;";
        const string gpuPreferenceRegValueDefault = "GpuPreference=0;";
        //const string GPU_PREFERENCE_REG_VALUE_POWER_SAVING = "GpuPreference=1;"

        var isCurrentHighPerformance = false;
        //查看现有设置
        using (var readOnlyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(gpuPreferenceRegKey, false))
        {
            if (readOnlyKey != null)
            {
                var currentValue = readOnlyKey.GetValue(executable);
                if (gpuPreferenceRegValueHigh == currentValue?.ToString())
                {
                    isCurrentHighPerformance = true;
                }
            }
            else
            {
                //创建父级键
                LogWrapper.Info("System", "需要创建显卡设置的父级键");
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey(gpuPreferenceRegKey);
            }
        }
        LogWrapper.Info("System", $"当前程序 ({executable}) 的显卡设置为高性能: {isCurrentHighPerformance}");
        if (!(isCurrentHighPerformance ^ wantHighPerformance)) return;
        
        //写入新设置
        using (var writeKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(gpuPreferenceRegKey, true))
        {
            writeKey?.SetValue(executable, wantHighPerformance 
                ? gpuPreferenceRegValueHigh 
                : gpuPreferenceRegValueDefault);
            LogWrapper.Info("System", $"已调整程序 ({executable}) 显卡设置: {wantHighPerformance}");
        }
    }
}

[ArgumentHandler]
public class GpuHandler(): GeneralHandler("gpu")
{
    public override HandleResult Handle(string[] args)
    {
        if (args is not [_, var mode]) return HandleResult.NotHandled;
        try
        {
            GpuHelper.SetGpuPreference(mode.Trim('\"'));
            return HandleResult.HandledAndExit;
        }
        catch (Exception e)
        {
            ParentContext.Fatal("调整显卡设置时发生错误", e);
            return HandleResult.HandledAndExit;
        }
    }
}