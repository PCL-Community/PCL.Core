using System;
using PCL.Core.Minecraft.Instance.InstanceImpl.JsonBased.Merge;
using PCL.Core.Minecraft.Instance.InstanceImpl.JsonBased.Patch;
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
            MergeInstance noPatchesInstance => CopyCommonProperties(noPatchesInstance, new MergeInstance(noPatchesInstance.Path)),
            PatchInstance patchesInstance => CopyCommonProperties(patchesInstance, new PatchInstance(patchesInstance.Path)),
            _ => throw new NotSupportedException("不支持的实例类型")
        };
    }
}
