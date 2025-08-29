using System;
using System.Reflection;

namespace PCL.Core.IO;

public class Paths {
    /// <summary>
    /// 程序可执行文件所在目录，以“\”结尾。
    /// </summary>
    public static string Path { get; } = GetExecutableDirectory();

    /// <summary>
    /// 获取可执行文件所在目录，确保以“\”结尾。
    /// </summary>
    private static string GetExecutableDirectory() {
        string location = Assembly.GetExecutingAssembly().Location;
        string directory = System.IO.Path.GetDirectoryName(location) ?? throw new InvalidOperationException("无法获取可执行文件目录");
        return directory.EndsWith("\\") ? directory : directory + "\\";
    }
}
