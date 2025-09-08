using System.Dynamic;

namespace PCL.Core.Minecraft.Instance.Interface;

public interface IMcInstanceBasic {
    /// <summary>
    /// 实例文件夹路径，以“\”结尾
    /// </summary>
    string Path { get; }

    /// <summary>
    /// 实例文件夹名称
    /// </summary>
    string Name { get; }
    
    McInstanceCardType CardType { get; set; }
    
    /// <summary>
    /// 显示的实例描述文本
    /// </summary>
    string Desc { get; set; }
    
    /// <summary>
    /// 显示的实例图标路径
    /// </summary>
    string Logo { get; set; }
    
    /// <summary>
    /// 实例是否被收藏
    /// </summary>
    bool IsStarred { get; }
}
