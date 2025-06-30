using System;

namespace PCL.Core.ProgramSetup.FileManager;

/// <summary>
/// 用于托管某个位于游戏实例文件夹内的配置文件的类，同步地写入文件
/// </summary>
public sealed class InstanceSetupFileManager : ISetupFileManager
{
    public string? Get(string key, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        throw new NotImplementedException();
    }

    public string? Set(string key, string value, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        throw new NotImplementedException();
    }

    public string? Remove(string key, string? mcPath)
    {
        if (mcPath is null)
            throw new ArgumentNullException(nameof(mcPath));
        throw new NotImplementedException();
    }
}