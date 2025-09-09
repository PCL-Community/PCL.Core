using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using PCL.Core.IO;
using PCL.Core.Logging;
using PCL.Core.Minecraft.Instance.Interface;

namespace PCL.Core.Minecraft.Instance.Handler.JsonBased;

public static class InstanceInfoHandler {
    private readonly FrozenDictionary<string, string> _patcherIdNameMapping = new Dictionary<string, string> {
            { "org.quiltmc:quilt-loader", "quilt" },
            { "com.cleanroommc:cleanroom", "cleanroom" },
            { "com.mumfrey:liteloader", "liteloader" },
        }
        .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    
    private static IMcInstance? RefreshInstanceInfo(IMcInstance instance, JsonObject versionJson, JsonObject libraries) {
        var clonedInstance = McInstanceFactory.CloneInstance(instance);
        
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
            clonedInstance.Desc = "无法识别 Minecraft 版本";
        }

        // 获取发布时间
        var releaseTime = McInstanceUtils.RecognizeReleaseTime(versionJson);
        instanceInfo.ReleaseTime = releaseTime;

        // 获取版本类型
        instanceInfo.VersionType = McInstanceUtils.RecognizeVersionType(versionJson, releaseTime);
        
        try {
            if (IsPatchesFormatJson) {
                foreach (var patch in versionJson!["patches"]!.AsArray()) {
                    var patcherInfo = patch.Deserialize<PatcherInfo>(Files.PrettierJsonOptions);
                    if (patcherInfo != null) {
                        instanceInfo.Patchers.Add(patcherInfo);
                    }
                }
            }
        } catch (Exception ex) {
            LogWrapper.Warn(ex, "识别 Minecraft 版本时出错");
            instanceInfo.McVersionStr = "Unknown";
            clonedInstance.Desc = $"无法识别：{ex.Message}";
        }
        
        clonedInstance.InstanceInfo = instanceInfo;

        return clonedInstance;
    }
    
    private void ConvertToPatches() {
        ParseLibraryNamesAsHashSet();

        // Quilt & Cleanroom & LiteLoader
        foreach (var pair in _patcherIdNameMapping) {
            var version = FindPatcherVersionsInHashSet(pair.Key);
            if (version != null) {
                _instanceInfo!.Patchers.Add(new PatcherInfo {
                    Id = pair.Value,
                    Version = version
                });
            }
        }

        var hasNeoForge = true;

        // NeoForge
        if (FindPatcherVersionsInHashSet("net.neoforged.fancymodloader") != null) {
            try {
                if (_instanceInfo!.McVersionStr.IsNullOrEmpty()) {
                    try {
                        FindArgumentData("--fml.neoForgeVersion", "neoforge");
                    } catch {
                        FindArgumentData("--fml.forgeVersion", "neoforge");
                    }
                }
                FindArgumentData(_instanceInfo!.McVersionStr == "1.20.1" ? "--fml.forgeVersion" : "--fml.neoForgeVersion", "neoforge");

            } catch (Exception ex) {
                hasNeoForge = false;
                LogWrapper.Warn(ex, "识别 NeoForge 时出错");
            }
        } else {
            hasNeoForge = false;
        }

        var hasFabric = false;
        if (!hasNeoForge) {
            // Fabric & LegacyFabric
            var fabricVersion = FindPatcherVersionsInHashSet("net.fabricmc:fabric-loader");
            if (fabricVersion != null) {
                if (FindPatcherVersionsInHashSet("net.legacyfabric") != null) {
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
                        Id = "legacyfabric",
                        Version = fabricVersion
                    });
                } else {
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
                        Id = "fabric",
                        Version = fabricVersion
                    });
                }
                hasFabric = true;
            }
        }

        if (!hasNeoForge & !hasFabric) {
            // Forge
            try {
                FindArgumentData("--fml.forgeVersion", "forge");
            } catch (Exception ex) {
                LogWrapper.Warn(ex, "识别 Forge 时出错");
            }
        }

        // OptiFine
        var optiFineVersion = FindPatcherVersionsInHashSet("optifine:OptiFine");
        if (optiFineVersion != null) {
            var parts = optiFineVersion.Split('_', 2);
            if (parts.Length > 1) {
                if (Version.TryParse(parts[0], out _)) {
                    _instanceInfo!.Patchers.Add(new PatcherInfo {
                        Id = "optifine",
                        Version = parts[1]
                    });
                }
            }
        }

        // LabyMod
        try {
            // 使用 FirstOrDefault() 查找符合条件的节点
            var labyModNode = _versionJson!["arguments"]!["game"]!.AsArray()
                .FirstOrDefault(node =>
                    node!.GetValueKind() == JsonValueKind.String &&
                    node.ToString().Contains("labymod", StringComparison.OrdinalIgnoreCase));
            if (labyModNode != null) {
                _instanceInfo!.Patchers.Add(new PatcherInfo {
                    Id = "labymod"
                });
            }
        } catch {
            LogWrapper.Info("未识别到 LabyMod");
        }
        
        // Game
        _instanceInfo!.Patchers.Add(new PatcherInfo {
            Id = "game",
            Version = _instanceInfo.McVersionStr
        });
    }
    
    private static void FindArgumentData(string argument, string id) {
        var args = _versionJson!["arguments"]!["game"]!.AsArray();
        var index = args.IndexOf(argument);
        var version = args[index + 1];
        _instanceInfo!.Patchers.Add(new PatcherInfo {
            Id = id,
            Version = version!.ToString()
        });
    }
    
    /// <summary>
    /// 从 JSON 提取 libraries 的 name 属性为 HashSet
    /// </summary>
    private static void ParseLibraryNamesAsHashSet() {
        _libraryNameHashCache = _libraries!.Where(lib => lib.Name != null)
            .Select(lib => lib.Name!)
            .ToHashSet();
    }
}
