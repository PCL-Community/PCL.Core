namespace PCL.Core.App.Configuration.Implementations;

/// <summary>
/// 键值对操作执行类型。
/// </summary>
public enum TrafficAccess
{
    /// <summary>
    /// 检查键是否存在
    /// </summary>
    CheckExists,

    /// <summary>
    /// 读取值。
    /// </summary>
    Read,

    /// <summary>
    /// 写入值。
    /// </summary>
    Write,

    /// <summary>
    /// 删除值。
    /// </summary>
    Delete
}
