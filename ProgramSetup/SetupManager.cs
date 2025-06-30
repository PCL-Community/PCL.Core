using System;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 设置模块的对外接口
/// </summary>
public static class SetupManager
{
    public static Setup Setup => throw new NotImplementedException();

    public static SetupFileManager GlobalSetupFile => throw new NotImplementedException();

    public static SetupFileManager LocalSetupFile => throw new NotImplementedException();

    public static InstanceSetupFileManager InstanceSetupFile => throw new NotImplementedException();
}