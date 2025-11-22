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
    All,
    /// <summary>
    /// 模组
    /// </summary>
    Mod,
    /// <summary>
    /// 模组包
    /// </summary>
    Modpack,
    /// <summary>
    /// 资源包（材质包）
    /// </summary>
    ResourcePack,
    /// <summary>
    /// 数据包
    /// </summary>
    DataPack,
    /// <summary>
    /// 光影
    /// </summary>
    Shader,
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
    /// <summary>
    /// Forge
    /// </summary>
    Forge,
    /// <summary>
    /// Fabric
    /// </summary>
    Fabric,
    /// <summary>
    /// LiteLoader
    /// </summary>
    LiteLoader
}

/// <summary>
/// 资源的发布类型，例如 Beta、Release 等
/// </summary>
public enum ReleaseType{
    Alapha,
    Beta,
    Release
}

/// <summary>
/// 通用 Tag
/// </summary>
public enum CommonTag
{
    /// <summary>
    /// 不筛选
    /// </summary>
    Nothing,
    /// <summary>
    /// 科技
    /// </summary>
    Technology,
    /// <summary>
    /// 魔法
    /// </summary>
    Magic,
    /// <summary>
    /// 冒险
    /// </summary>
    Adventure,
    /// <summary>
    /// 实用工具
    /// </summary>
    Utility,
    /// <summary>
    /// 性能优化
    /// </summary>
    Optimizationags,
    /// <summary>
    /// 原版风
    /// </summary>
    VanillaLike,
    /// <summary>
    /// 写实风
    /// </summary>
    Realistic,
}

/// <summary>
/// Mod & 数据包标签
/// </summary>
public enum ModOrDataoackTag{
    /// <summary>
    /// 世界元素
    /// </summary>
    Worldgen,
    /// <summary>
    /// 食物/烹饪
    /// </summary>
    Food,
    /// <summary>
    /// 游戏机制
    /// </summary>
    GameMechanics,
    /// <summary>
    /// 运输
    /// </summary>
    Transportation,
    /// <summary>
    /// 仓储
    /// </summary>
    Storage,
    /// <summary>
    /// 装饰
    /// </summary>
    Decoration,
    /// <summary>
    /// 生物
    /// </summary>
    Mobs,
    /// <summary>
    /// 装备
    /// </summary>
    Equipment,
    /// <summary>
    /// 服务器
    /// </summary>
    Social,
    /// <summary>
    /// 支持库
    /// </summary>
    Library
}

/// <summary>
/// 整合包标签
/// </summary>
public enum ModpackTag
{
    /// <summary>
    /// 多人
    /// </summary>
    MultiPlayer,             
    /// <summary>
    /// 硬核
    /// </summary>
    Challenging,             
    /// <summary>
    /// 战斗
    /// </summary>
    Combat,
    /// <summary>
    /// 任务
    /// </summary>
    Quests,
    /// <summary>
    /// 水槽包
    /// </summary>
    KitchenSink,
    /// <summary>
    /// 轻量
    /// </summary>
    Lightweight
}


public enum ResourcePackTag
{
    /// <summary>
    /// 简介
    /// </summary>
    Simplistic,
    /// <summary>
    /// 战斗
    /// </summary>
    Combat,
    /// <summary>
    /// 改良
    /// </summary>
    Tweaks,
    /// <summary>
    /// 极简 （8x-）
    /// </summary>
    R8OrLower,
    /// <summary>
    /// 16px
    /// </summary>
    R16,
    /// <summary>
    /// 32px
    /// </summary>
    R32,
    /// <summary>
    /// 48px
    /// </summary>
    R48,
    /// <summary>
    /// 64px
    /// </summary>
    R64,
    /// <summary>
    /// 128px
    /// </summary>
    R128,
    /// <summary>
    /// 256px
    /// </summary>
    R256,
    /// <summary>
    /// 超高清 （512px+）
    /// </summary>
    R512OrHigher,
    /// <summary>
    /// 含声音
    /// </summary>
    Audio,
    /// <summary>
    /// 含字体
    /// </summary>
    Fonts,
    /// <summary>
    /// 含 UI
    /// </summary>
    Gui,
    /// <summary>
    /// 含模型
    /// </summary>
    Models,
    /// <summary>
    /// 含语言
    /// </summary>
    Locale,
    /// <summary>
    /// 核心着色器
    /// </summary>
    CoreShaders,
    /// <summary>
    /// 兼容 Mod
    /// </summary>
    Modded
}

public enum ShadowTag
{
    /// <summary>
    /// 幻想风
    /// </summary>
    Fantasy,
    /// <summary>
    /// 半写实风
    /// </summary>
    SemiRealistic,
    /// <summary>
    /// 卡通风
    /// </summary>
    Cartoon,
    /// <summary>
    /// 极低
    /// </summary>
    Potato,
    /// <summary>
    /// 低
    /// </summary>
    Low,
    /// <summary>
    /// 中
    /// </summary>
    Medium,
    /// <summary>
    /// 高
    /// </summary>
    High,
    /// <summary>
    /// 彩色光照
    /// </summary>
    ColoredLighting,
    /// <summary>
    /// 路径追踪
    /// </summary>
    PathTracing,
    /// <summary>
    /// PBR
    /// </summary>
    PBR,
    /// <summary>
    /// 反射
    /// </summary>
    Reflections,
    /// <summary>
    /// Iris
    /// </summary>
    Iris,
    /// <summary>
    /// OptiFine
    /// </summary>
    OptiFine,
    /// <summary>
    /// 原版可用
    /// </summary>
    Vanilla
}


/// <summary>
/// 是否支持此平台
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

