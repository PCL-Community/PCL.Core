using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.Projects.Enums;
using PCL.Core.Minecraft.Compoment.Projects.Models;

namespace PCL.Core.Minecraft.Compoment.Projects;

public static class ProjectFactory
{
    private static readonly Dictionary<int, string> _CurseForgeProjectTags = new()
    {
        // Mod
        { 406, "世界元素" }, { 407, "生物群系" }, { 410, "维度" },
        { 408, "矿物/资源" }, { 409, "天然结构" }, { 412, "科技" },
        { 415, "管道/物流" }, { 4843, "自动化" }, { 417, "能源" },
        { 4558, "红石" }, { 436, "食物/烹饪" }, { 416, "农业" },
        { 414, "运输" }, { 420, "仓储" }, { 419, "魔法" },
        { 422, "装饰" }, { 411, "生物" }, { 434, "装备" },
        { 423, "信息显示" }, { 435, "服务器" }, { 5191, "改良" },
        { 421, "支持库" },
        // Mod Pack
        { 4484, "多人" }, { 4479, "硬核" }, { 4483, "战斗" },
        { 4478, "任务" }, { 4472, "科技" }, { 4473, "魔法" },
        { 4475, "冒险" }, { 4476, "探索" }, { 4477, "小游戏" },
        { 4471, "科幻" }, { 4736, "空岛" }, { 5128, "原版改良" },
        { 4487, "FTB" }, { 4480, "基于地图" }, { 4481, "轻量" },
        { 4482, "大型" },
        // Resource Pack
        { 403, "原版风" },
        { 400, "写实风" }, { 401, "现代风" }, { 402, "中世纪" },
        { 399, "蒸汽朋克" }, { 5244, "含字体" }, { 404, "动态效果" },
        { 4465, "兼容 Mod" }, { 393, "16x" }, { 394, "32x" },
        { 395, "64x" }, { 396, "128x" }, { 397, "256x" },
        { 398, "超高清" }, { 5193, "数据包" },
        // Shader Pack
        { 6553, "写实风" }, { 6554, "幻想风" }, { 6555, "原版风" },
        // Data Pack
        { 6948, "冒险" }, { 6949, "幻想" }, { 6950, "支持库" },
        { 6952, "魔法" }, { 6946, "Mod 相关" }, { 6951, "科技" },
        { 6953, "实用" },
        // World
        { 248, "冒险" }, { 249, "创造" }, { 250, "小游戏" },
        { 251, "跑酷" }, { 252, "解谜" }, { 253, "生存" },
        { 4464, "Mod 世界" },
    };

    private static readonly Dictionary<string, string> _ModrinthProjectTags = new()
    {
        // Loader
        //{ "fabric", "Fabric" }, { "forge", "Forge" },
        //{ "quilt", "Quilt" }, { "neoforge", "NeoForge" },
        //{ "datapack", "DataPack" },
        // Both
        { "technology", "科技" }, { "magic", "魔法" }, { "adventure", "冒险" },
        { "utility", "实用" }, { "optimization", "性能优化" }, { "vanilla-like", "原版风" },
        { "realistic", "写实风" },
        // Mod/DataPack
        { "worldgen", "世界元素" }, { "food", "食物/烹饪" },
        { "game-mechanics", "游戏机制" }, { "transportation", "运输" }, { "storage", "仓储" },
        // NOTE: 'decoration', 'mobs' and 'equipment' have special handling
        { "decoration", "装饰" }, { "mobs", "生物" }, { "equipment", "装备" },
        { "social", "服务器" }, { "library", "支持库" },
        // ModPack
        { "multiplayer", "多人" }, { "challenging", "硬核" }, { "combat", "战斗" },
        { "questing", "任务" }, { "kitchen-sink", "水槽包" }, { "lightweight", "轻量" },
        // ResourcePack
        { "simplistic", "简洁" }, { "tweaks", "改良" }, { "8x-", "极简" },
        { "16x", "16x" }, { "32x", "32x" }, { "64x", "64x" },
        { "128x", "128x" }, { "256x", "256x" }, { "512x+", "超高清" },
        { "audio", "含声音" }, { "fonts", "含字体" }, { "models", "含模型" },
        { "gui", "含 UI" }, { "locale", "含语言" }, { "core-shaders", "核心着色器" },
        { "modded", "兼容 Mod" },
        // ShaderPack
        { "fantasy", "幻想风" }, { "semi-realistic", "半写实风" }, { "cartoon", "卡通风" },
        { "colored-lighting", "彩色光照" }, { "path-tracing", "路径追踪" }, { "pbr", "PBR" },
        { "reflections", "反射" }, { "iris", "Iris" }, { "optifine", "OptiFine" },
        { "vanilla", "原版可用" }
    };

    public static ProjectInfo CreateFromCurseForgeJson(string jsonContent)
    {
        var dto = JsonSerializer.Deserialize<CurseForgeProjectDto>(jsonContent)
                  ?? throw new ArgumentException("Invalid CurseForge JSON content.", nameof(jsonContent));

        var type = _GetCompTypeFromCurseForgeWebsite(dto.Links.WebsiteUrl);

        var allFiles = dto.LatestFiles
            .Select(fl => new { fl.Id, fl.GameVersions })
            .Concat(dto.LatestFilesIndexes
                .Select(fl => new { Id = fl.FileId, GameVersions = new List<string> { fl.GameVersion } }))
            .ToList();

        var curseForgeFileIds = allFiles.Select(it => it.Id).Distinct().ToList();

        var gameVersions = allFiles
            .SelectMany(fl => fl.GameVersions)
            .Where(ver => ver.StartsWith("1."))
            .Select(ver => ver.Split('.')[1].Split('-')[0])
            .Select(ver => int.TryParse(ver, out var result) ? result : -1)
            .Where(ver => ver > 0)
            .Distinct()
            .OrderByDescending(ver => ver)
            .ToList();

        var tags = dto.Categories
            .Select(ca => ca.Id)
            .Where(_CurseForgeProjectTags.ContainsKey)
            .Select(id => _CurseForgeProjectTags[id])
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();

        if (tags.Count == 0)
        {
            tags.Add("其他");
        }

        return new ProjectInfo
        {
            FromCurseForge = true,
            Type = type,
            Slug = dto.Slug,
            Id = dto.Id,
            CurseForgeFileIds = curseForgeFileIds,
            RawName = dto.Name,
            Description = dto.Summary,
            Website = dto.Links.WebsiteUrl.TrimEnd('/'),
            LastUpdate = dto.DateReleased,
            DownloadCount = dto.DownloadCount,
            LogoUrl = !string.IsNullOrEmpty(dto.Logo?.ThumbnailUrl) ? dto.Logo.ThumbnailUrl : dto.Logo?.Url,
            GameVersions = gameVersions,
            ModLoaders = [], // TODO: impl mod loader detection
            Tags = tags
        };
    }

    public static ProjectInfo CreateFromModrinthJson(string jsonContent)
    {
        var dto = JsonSerializer.Deserialize<ModrinthProjectDto>(jsonContent)
                  ?? throw new ArgumentException("Invalid Modrinth JSON content.", nameof(jsonContent));

        var type = dto.ProjectType switch
        {
            "modpack" => CompType.ModPack,
            "resourcepack" => CompType.ResourcePack,
            "shader" => CompType.Shader,
            _ => CompType.Mod
        };


        var categoriesSet = dto.Categories.ToHashSet();
        List<LoaderType> modLoaders = [];
        if (categoriesSet.Contains("forge"))
        {
            modLoaders.Add(LoaderType.Forge);
        }

        if (categoriesSet.Contains("fabric"))
        {
            modLoaders.Add(LoaderType.Fabric);
        }

        if (categoriesSet.Contains("quilt"))
        {
            modLoaders.Add(LoaderType.Quilt);
        }

        if (categoriesSet.Contains("neoforge"))
        {
            modLoaders.Add(LoaderType.NeoForge);
        }

        if (categoriesSet.Contains("datapack"))
        {
            type = CompType.DataPack;
        }

        var gameVersions = dto.EffectiveGameVersions
            .Where(ver => ver.StartsWith("1."))
            .Select(ver => ver.Split('.')[1].Split('-')[0])
            .Select(verS => int.TryParse(verS, out var result) ? result : -1)
            .Where(ver => ver > 0)
            .Distinct()
            .OrderByDescending(ver => ver)
            .ToList();

        if (type is CompType.ResourcePack)
        {
            categoriesSet.Remove("decoration");
            categoriesSet.Remove("mobs");
            categoriesSet.Remove("equipment");
        }

        var tags = categoriesSet
            .Where(_ModrinthProjectTags.ContainsKey)
            .Select(id => _ModrinthProjectTags[id])
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();

        return new ProjectInfo
        {
            FromCurseForge = false,
            Type = type,
            Slug = dto.Slug,
            Id = dto.EffectiveId,
            RawName = dto.Title,
            Description = dto.Description,
            Website = $"https://modrinth.com/{dto.ProjectType}/{dto.Slug}",
            LastUpdate = dto.DateModified,
            DownloadCount = dto.Downloads,
            LogoUrl = dto.IconUrl,
            GameVersions = gameVersions,
            ModLoaders = modLoaders,
            Tags = tags
        };
    }

    private static CompType _GetCompTypeFromCurseForgeWebsite(string url)
    {
        if (url.Contains("/mc-mods/") || url.Contains("/mod/"))
        {
            return CompType.Mod;
        }

        if (url.Contains("/modpacks/"))
        {
            return CompType.ModPack;
        }

        if (url.Contains("/resourcepacks/") || url.Contains("/texture-packs/"))
        {
            return CompType.ResourcePack;
        }

        if (url.Contains("/shaders/"))
        {
            return CompType.Shader;
        }

        if (url.Contains("/worlds/"))
        {
            return CompType.World;
        }

        return CompType.DataPack;
    }
}