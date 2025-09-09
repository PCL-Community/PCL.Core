using System;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Instance;

public class McInstanceFactory {
    public static IMcInstance CloneInstance(IMcInstance original) {
        // 这里使用类型判断来创建新的实例
        if (original is McNoPatchesInstance noPatchesInstance) {
            return new McNoPatchesInstance(noPatchesInstance.Path) {
                CardType = noPatchesInstance.CardType,
                Desc = noPatchesInstance.Desc,
                Logo = noPatchesInstance.Logo,
                InstanceInfo = noPatchesInstance.InstanceInfo,
            };
        }
        if (original is McPatchesInstance patchesInstance) {
            return new McPatchesInstance(){ /* 拷贝属性 */ };
        }
        throw new NotSupportedException("不支持的实例类型");
    }
}
