using System;

namespace PCL.Core.Minecraft.Instance.Interface;

public interface IMcInstanceInfo {
    /// <summary>
    /// 实例发布时间
    /// </summary>
    DateTime ReleaseTime { get; }

    /// <summary>
    /// 原版版本名，如 "1.12.2" 或 "16w01a"。
    /// </summary>
    string McVersionStr { get; }

    /// <summary>
    /// 可读的版本名
    /// </summary>
    string FormattedVersion { get; }

    /// <summary>
    /// MC 版本类型
    /// </summary>
    McVersionType VersionType { get; }
    
    bool HasPatcher(string id);

    string GetPatcherVersion(string id);
}
