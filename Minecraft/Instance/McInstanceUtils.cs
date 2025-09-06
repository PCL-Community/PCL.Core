using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PCL.Core.Minecraft.Instance;

public static class McInstanceUtils {
    private static readonly ImmutableDictionary<string, (int Year, string Description)> FoolVersionDescriptions =
        ImmutableDictionary.CreateRange(new Dictionary<string, (int Year, string Description)>
        {
            { "15w14a", (2015, "作为一款全年龄向的游戏，我们需要和平，需要爱与拥抱。") },
            { "1.rv-pre1", (2016, "是时候将现代科技带入 Minecraft 了！") },
            { "3d shareware v1.34", (2019, "我们从地下室的废墟里找到了这个开发于 1994 年的杰作！") },
            { "20w14∞", (2020, "我们加入了 20 亿个新的维度，让无限的想象变成了现实！") },
            { "22w13oneblockatatime", (2022, "一次一个方块更新！迎接全新的挖掘、合成与骑乘玩法吧！") },
            { "23w13a_or_b", (2023, "研究表明：玩家喜欢作出选择——越多越好！") },
            { "24w14potato", (2024, "毒马铃薯一直都被大家忽视和低估，于是我们超级加强了它！") },
            { "25w14craftmine", (2025, "你可以合成任何东西——包括合成你的世界！") }
        });

    private static readonly ImmutableDictionary<string, string> VariantSuffixes =
        ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "red", "（红色版本）" },
            { "blue", "（蓝色版本）" },
            { "purple", "（紫色版本）" }
        });
    
    public static string GetMcFoolVersionDesc(string name) {
        name = name.ToLowerInvariant();

        // 精确匹配
        if (FoolVersionDescriptions.TryGetValue(name, out var match))
            return $"{match.Year} | {match.Description}";

        // 前缀匹配
        if (name.StartsWith("2.0") || name.StartsWith("2point0"))
            return $"2013 | 这个秘密计划了两年的更新将游戏推向了一个新高度！{GetVariantSuffix(name)}";

        return "";
    }
    
    private static string GetVariantSuffix(string name) {
        return VariantSuffixes.FirstOrDefault(s => name.EndsWith((string)s.Key)).Value ?? "";
    }
    
    public static string? RecognizeMcVersion(JsonObject versionJson) {
        // Get version from patches
        if (versionJson.TryGetPropertyValue("patches", out var patchesElement) &&
            patchesElement?.GetValueKind() == JsonValueKind.Array) {
            var patchesArray = (JsonArray) patchesElement;
            var gamePatch = patchesArray
                .OfType<JsonObject>()
                .FirstOrDefault(patch =>
                    patch.TryGetPropertyValue("id", out var idElement) &&
                    idElement?.ToString() == "game" &&
                    patch.TryGetPropertyValue("version", out _));

            if (gamePatch?.TryGetPropertyValue("version", out var versionElement) == true) {
                var version = versionElement?.ToString();
                if (!string.IsNullOrEmpty(version)) {
                    return version;
                }
            }
        }

        return versionJson.TryGetPropertyValue("clientVersion", out var clientVersionElement) ? clientVersionElement!.ToString() : null;
    }
    
    /// <summary>
    /// 异步获取版本的发布日期时间，如果无法获取或解析失败，则返回默认时间（1970-01-01 15:00:00）。
    /// </summary>
    /// <returns>版本的发布日期时间，或默认时间。</returns>
    public static DateTime RecognizeReleaseTime(JsonObject jsonObject) {
        if (!jsonObject.TryGetPropertyValue("releaseTime", out var releaseTimeNode) || 
            releaseTimeNode == null || 
            !DateTime.TryParse(releaseTimeNode.GetValue<string>(), out var releaseTime))
        {
            return DateTime.MinValue;
        }

        return releaseTime;
    }
    
    public static McVersionType RecognizeVersionType(JsonObject versionJson, DateTime releaseTime) {
        if (releaseTime is { Month: 4, Day: 1 }) {
            return McVersionType.Fool;
        }
        
        if (releaseTime.Year > 2000 && releaseTime <= new DateTime(2011, 11, 16)) {
            return McVersionType.Old;
        }
        
        if (versionJson.TryGetPropertyValue("type", out var typeElement)) {
            var typeString = typeElement!.GetValue<string>();
            if (typeString == "release") {
                return McVersionType.Release;
            }
        }
        return McVersionType.Snapshot;
    }
    
    public static McInstanceCardType? RecognizeInstanceCardType(McInstanceInfo instanceInfo) {
        McInstanceCardType? cachedDisplayType = null;
        
        if (instanceInfo.HasPatcher("NeoForge")) {
            cachedDisplayType = McInstanceCardType.NeoForge;
        } else if (instanceInfo.HasPatcher("Fabric")) {
            cachedDisplayType = McInstanceCardType.Fabric;
        } else if (instanceInfo.HasPatcher("LegacyFabric")) {
            cachedDisplayType = McInstanceCardType.LegacyFabric;
        } else if (instanceInfo.HasPatcher("Quilt")) {
            cachedDisplayType = McInstanceCardType.Quilt;
        } else if (instanceInfo.HasPatcher("Forge")) {
            cachedDisplayType = McInstanceCardType.Forge;
        } else if (instanceInfo.HasPatcher("Cleanroom")) {
            cachedDisplayType = McInstanceCardType.Cleanroom;
        } else if (instanceInfo.HasPatcher("LiteLoader")) {
            cachedDisplayType = McInstanceCardType.LiteLoader;
        } 
        
        // 判断客户端类型的补丁实例
        else if (instanceInfo.HasPatcher("OptiFine")) {
            cachedDisplayType = McInstanceCardType.OptiFine;
        } else if (instanceInfo.HasPatcher("LabyMod")) {
            cachedDisplayType = McInstanceCardType.LabyMod;
        } else if (instanceInfo.HasPatcher("Client")) {
            cachedDisplayType = McInstanceCardType.Client;
        }

        return cachedDisplayType;
    }
}
