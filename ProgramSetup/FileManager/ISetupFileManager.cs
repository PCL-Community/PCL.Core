namespace PCL.Core.ProgramSetup.FileManager;

public interface ISetupFileManager
{
    /// <summary>
    /// 获取或设置某个键对应的值 <br/>
    /// 获取时若键不存在则返回空，设置时若传入空则删除键
    /// </summary>
    string? this[string key, string? mcPath] { get; set; }
}