namespace PCL.Core.Minecraft.ResourceProject.ResourcePlatform.Models;

/// <summary>
/// 资源信息的来源平台
/// </summary>
public enum ProjectSource
{
    Unknown,
    Modrinth,
    CurseForge
}

/// <summary>
/// 资源类型
/// </summary>
public enum ResourceType
{
    
}

/// <summary>
/// Mod 加载器类型
/// </summary>
public enum ModLoaderType
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown,
    /// <summary>
    /// 文件覆盖
    /// </summary>
    Native,
    Forge
}

/// <summary>
/// 资源的发布类型，例如 Beta、Release 等
/// </summary>
public enum ReleaseType{

}

/// <summary>
/// Mod 的标签，例如性能优化、支持库
/// </summary>
public enum ModTag{

}
/// <summary>
/// 资源是否需要
/// </summary>
public enum ResourceRequire
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown,
    /// <summary>
    /// 必须安装
    /// </summary>
    Required,
    /// <summary>
    /// 可选
    /// </summary>
    Optional,
    /// <summary>
    /// 不支持此平台
    /// </summary>
    Unsupported
}

