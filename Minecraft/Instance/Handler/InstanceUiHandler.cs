using System.Collections.Immutable;
using PCL.Core.App;
using PCL.Core.Minecraft.Instance.Interface;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Instance.Handler;

public static class InstanceUiHandler {
    private static readonly ImmutableArray<string> DescStrings = [
        "开启一段全新的冒险之旅！",
        "创造属于你的独特世界。",
        "探索无尽的可能性。",
        "随时随地，开始你的旅程。",
        "打造你的梦想之地。",
        "自由发挥，享受无限乐趣。",
        "一个属于你的 Minecraft 故事。",
        "发现新奇，创造精彩。",
        "轻松开启，畅玩无忧。",
        "你的冒险，从这里起航。",
        "构建、探索、尽情享受！",
        "适合每一位玩家的乐园。",
        "创造与冒险的完美结合。",
        "开启属于你的游戏篇章。",
        "探索未知，创造奇迹。",
        "属于你的 Minecraft 世界。",
        "简单上手，乐趣无穷。",
        "打造你的专属冒险舞台。",
        "从零开始，创造无限。",
        "你的故事，等待书写！"
    ];

    /// <summary>
    /// 获得一个实例实际的描述文本
    /// </summary>
    public static string GetDescription(IMcInstance instance) {
        return string.IsNullOrEmpty(Config.Instance.CustomInfo[instance.Path])
            ? GetDefaultDescription(instance)
            : Config.Instance.CustomInfo[instance.Path];
    }
    
    /// <summary>
    /// 获得一个实例的默认描述文本
    /// </summary>
    private static string GetDefaultDescription(IMcInstance instance) {
        if (instance.CardType == McInstanceCardType.Error) {
            return "";
        }
        return instance.InstanceInfo.VersionType == McVersionType.Fool ? McInstanceUtils.GetMcFoolVersionDesc(instance.InstanceInfo.McVersionStr!) : RandomUtils.PickRandom(DescStrings);
    }
    
    public static string GetLogo(IMcInstance instance) {
        var logo = Config.Instance.LogoPath[instance.Path];
        if (string.IsNullOrEmpty(logo) || !Config.Instance.IsLogoCustom[instance.Path]) {
            return instance.InstanceInfo.GetLogo();
        }
        return logo;
    }
}
