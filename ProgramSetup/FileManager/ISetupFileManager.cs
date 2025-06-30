namespace PCL.Core.ProgramSetup.FileManager;

public interface ISetupFileManager
{
    string? this[string key, string? mcPath] { get; set; }
}