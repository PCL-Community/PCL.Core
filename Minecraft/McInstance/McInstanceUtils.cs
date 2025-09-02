using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PCL.Core.App;

namespace PCL.Core.Minecraft.McInstance;

public static class McInstanceUtils {
    private static readonly ImmutableDictionary<string, (int Year, string Description)> VersionDescriptions =
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
        if (VersionDescriptions.TryGetValue(name, out var match))
            return $"{match.Year} | {match.Description}";

        // 前缀匹配
        if (name.StartsWith("2.0") || name.StartsWith("2point0"))
            return $"2013 | 这个秘密计划了两年的更新将游戏推向了一个新高度！{GetVariantSuffix(name)}";
        if (name.StartsWith("20w14inf"))
            return "2020 | 我们加入了 20 亿个新的维度，让无限的想象变成了现实！";

        return "";
    }
    
    private static string GetVariantSuffix(string name) {
        return VariantSuffixes.FirstOrDefault(s => name.EndsWith(s.Key)).Value ?? "";
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

        // Get version from clientVersion
        if (versionJson.TryGetPropertyValue("clientVersion", out var clientVersionElement)) {
            return clientVersionElement!.ToString();
        }

        return null;
    }
}
