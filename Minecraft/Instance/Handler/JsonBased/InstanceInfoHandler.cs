using System;
using System.Text.Json.Nodes;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Instance.Handler;

public static class InstanceInfoHandler {
    private static McInstanceInfo? RefreshInstanceInfo(IMcInstance instance, JsonObject versionJson) {
        var instanceInfo = new McInstanceInfo();
        if (instance.CardType == McInstanceCardType.Error) {
            return null;
        }

        // 获取 MC 版本
        var version = McInstanceUtils.RecognizeMcVersion(versionJson);

        if (version != null) {
            instanceInfo.McVersionStr = version;
        } else {
            LogWrapper.Warn("识别 Minecraft 版本时出错");
            instanceInfo.McVersionStr = "Unknown";
            Desc = "无法识别 Minecraft 版本";
        }

        // 获取发布时间
        var releaseTime = McInstanceUtils.RecognizeReleaseTime(_versionJson!);
        instanceInfo.ReleaseTime = releaseTime;

        // 获取版本类型
        instanceInfo.VersionType = McInstanceUtils.RecognizeVersionType(_versionJson!, releaseTime);
        
        try {
            if (IsPatchesFormatJson) {
                foreach (var patch in _versionJson!["patches"]!.AsArray()) {
                    var patcherInfo = patch.Deserialize<PatcherInfo>(Files.PrettierJsonOptions);
                    if (patcherInfo != null) {
                        instanceInfo.Patchers.Add(patcherInfo);
                    }
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            instanceInfo.McVersionStr = "Unknown";
            Desc = $"无法识别：{ex.Message}";
        }
        _instanceInfo = instanceInfo;

        return _instanceInfo;
    }
}
