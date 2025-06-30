namespace PCL.Core.ProgramSetup;

public interface ISetupFileManager
{
    string? this[string key, string? mcPath] { get; set; }
}