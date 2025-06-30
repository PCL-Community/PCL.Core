using System;
using PCL.Core.ProgramSetup.FileManager;

namespace PCL.Core.ProgramSetup;

/// <summary>
/// 设置模块的对外接口
/// </summary>
public static class SetupManager
{
    public static SetupModel Setup => throw new NotImplementedException();

    public static CommonSetupFileManager GlobalSetupFile => throw new NotImplementedException();

    public static CommonSetupFileManager LocalSetupFile => throw new NotImplementedException();

    public static InstanceSetupFileManager InstanceSetupFile => throw new NotImplementedException();
}