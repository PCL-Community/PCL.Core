using System;
using System.Reflection;

namespace PCL.Core.IO;

public class Paths {
    /// <summary>
    /// 程序可执行文件所在目录，以“\”结尾。
    /// </summary>
    public static string ExePath { get; } = GetExecutableDirectory();
    
    /// <summary>
    /// 程序可执行文件的完整路径。
    /// </summary>
    public static string ExePathWithName { get; } = Assembly.GetExecutingAssembly().Location;

    /// <summary>
    /// 获取可执行文件所在目录，确保以“\”结尾。
    /// </summary>
    private static string GetExecutableDirectory() {
        var location = Assembly.GetExecutingAssembly().Location;
        var directory = System.IO.Path.GetDirectoryName(location) ?? throw new InvalidOperationException("无法获取可执行文件目录");
        return directory.EndsWith("\\") ? directory : directory + "\\";
    }
}
