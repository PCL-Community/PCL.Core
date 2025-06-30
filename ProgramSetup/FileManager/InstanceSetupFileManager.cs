using System;

namespace PCL.Core.ProgramSetup.FileManager;

public sealed class InstanceSetupFileManager : ISetupFileManager
{
    string? ISetupFileManager.this[string key, string? mcPath]
    {
        get => this[key, mcPath ?? throw new ArgumentNullException(nameof(mcPath))];
        set => this[key, mcPath ?? throw new ArgumentNullException(nameof(mcPath))] = value;
    }

    public string? this[string key, string mcPath]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}