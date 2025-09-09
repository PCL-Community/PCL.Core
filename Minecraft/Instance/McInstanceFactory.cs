using System;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Instance;

public static class McInstanceFactory {
    private static T CopyCommonProperties<T>(T source, T destination)
        where T: class, IMcInstance {
        destination.CardType = source.CardType;
        destination.Desc = source.Desc;
        destination.Logo = source.Logo;
        destination.InstanceInfo = source.InstanceInfo;
        return destination;
    }

    public static IMcInstance CloneInstance(IMcInstance original) {
        return original switch {
            McNoPatchesInstance noPatchesInstance => CopyCommonProperties(noPatchesInstance, new McNoPatchesInstance(noPatchesInstance.Path)),
            McPatchesInstance patchesInstance => CopyCommonProperties(patchesInstance, new McPatchesInstance(patchesInstance.Path)),
            _ => throw new NotSupportedException("不支持的实例类型")
        };
    }
}
