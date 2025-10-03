using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using PCL.Core.Minecraft.Compoment.Projects.Entities;
using PCL.Core.Minecraft.Compoment.Projects.Enums;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Compoment.Projects;

public static class ProjectFileFactory
{
    public static ProjectFileInfo Create(string rawJsonContent, CompType defaultType)
    {
        if (rawJsonContent.Contains("FromCurseForge"))
        {
            return _CreateFromCache(rawJsonContent);
        }

        if (rawJsonContent.Contains("gameId"))
        {
            return _CreateFromCurseForge(rawJsonContent);
        }
        else
        {
            return _CreateFromModrinth(rawJsonContent, defaultType);
        }
    }

    private static ProjectFileInfo _CreateFromCache(string rawJsonContent)
    {
        var dto = JsonSerializer.Deserialize<CacheProjectFileDot>(rawJsonContent)
                  ?? throw new ArgumentException("Invalid Cache JSON content.", nameof(rawJsonContent));

        var fileName = dto.FileName ?? string.Empty;
        var downloadUrls = dto.DownloadUrls ?? [];
        var modLoaders = dto.ModLoaders ?? [];
        var hash = dto.Hash ?? string.Empty;
        var gameVersions = dto.GameVersions ?? [];
        var rawDependencies = dto.RawDependencies ?? [];
        var dependencies = dto.Dependencies ?? [];
        var rawOptionalDependencies = dto.RawOptionalDependencies ?? [];
        var optionalDenpendencies = dto.OptionalDependencies ?? [];

        var info = new ProjectFileInfo
        {
            FromCurseForge = true,
            Id = dto.Id,
            DisplayName = dto.DisplayName,
            ReleaseDate = dto.ReleaseDate,
            DownloadCount = dto.DownloadCount,
            Status = dto.Status,
            FileName = fileName,
            DownloadUrls = downloadUrls,
            ModLoaders = modLoaders,
            Hash = hash,
            GameVersions = gameVersions,
            RawDependencies = rawDependencies,
            RawOptionalDependencies = rawOptionalDependencies,
            Dependencies = dependencies,
            Type = CompType.Mod,
            OptionalDependencies = optionalDenpendencies
        };

        return info;
    }

    private static ProjectFileInfo _CreateFromCurseForge(string rawJsonContent)
    {
        var dto = JsonSerializer.Deserialize<CurseForgeProjectFileDto>(rawJsonContent)
                  ?? throw new ArgumentException("Invalid CurseForge JSON content.", nameof(rawJsonContent));

        return CreateFromCurseForge(dto);
    }

    public static ProjectFileInfo CreateFromCurseForge(CurseForgeProjectFileDto dto)
    {
        var hash = dto.Hashes
            .Where(hash => hash.Algo is 1 or 2)
            .OrderBy(hash => hash.Algo)
            .FirstOrDefault()?.Value ?? string.Empty;

        var url = dto.DownloadUrl;
        if (string.IsNullOrEmpty(url))
        {
            url =
                $"https://edge.forgecdn.net/files/{dto.Id[..4]}/{dto.Id[4..]}/{WebUtility.UrlEncode(dto.FileName)}"
                    .Replace("+", "%20");
        }

        url = UrlConverter.HandleCurseForgeDownloadUrl(url); // TODO: impl mirror

        var rawDeps = new List<string>();
        var rawOptionalDeps = new List<string>();
        if (dto.Dependencies is not null)
        {
            var tempDpes = dto.Dependencies
                .Where(dep => dep is { ModId: not 306612 and not 634179 })
                .GroupBy(keySelector: dep => dep.RelationType, elementSelector: dep => dep.ModId)
                .ToDictionary(keySelector: group => group.Key,
                    elementSelector: group => group.ToList());

            if (tempDpes.TryGetValue(3, out var requires))
            {
                rawDeps.AddRange(requires.Select(id => id.ToString()));
            }

            if (tempDpes.TryGetValue(2, out var optionals))
            {
                rawOptionalDeps.AddRange(optionals.Select(id => id.ToString()));
            }
        }

        var rawGameVers = dto.GameVersions?.Select(ver => ver.Trim().ToLowerInvariant()).ToHashSet() ?? [];
        var gameVers = rawGameVers
            .Where(ver => ver.StartsWith("1."))
            .Select(ver => ver.Replace("-snapshot", "预览版"))
            .ToList();

        var sortedGameVers = Utils.GameVersionSorterUtil.SortGameVersions(gameVers);

        var modLoaders = ModLoaderDetector.DetechCurseForgeType(rawGameVers);

        var info = new ProjectFileInfo
        {
            Id = dto.Id,
            ProjectId = dto.ProjectId,
            DisplayName = dto.DisplayName,
            ReleaseDate = dto.ReleaseDate,
            Status = dto.ReleaseType,
            DownloadCount = dto.DownloadCount,
            FileName = dto.FileName,
            Hash = hash,
            DownloadUrls = [url],
            RawDependencies = rawDeps,
            Dependencies = [], // NOTE: lm doesnt impl this
            RawOptionalDependencies = rawOptionalDeps,
            OptionalDependencies = [], // NOTE: lm doesnt impl this
            GameVersions = sortedGameVers,
            Type = CompType.Mod,
            ModLoaders = modLoaders
        };

        return info;
    }

    private static ProjectFileInfo _CreateFromModrinth(string rawJsonContent, CompType defaultType)
    {
        var dto = JsonSerializer.Deserialize<ModrinthProjectFileDto>(rawJsonContent)
                  ?? throw new ArgumentException("Invalid Modrinth JSON content.", nameof(rawJsonContent));

        var displayName = dto.Name.Replace("	", string.Empty).Trim(' ');
        var status = dto.VersionType switch
        {
            "release" => ProjectFileStatus.Release,
            "beta" => ProjectFileStatus.Beta,
            _ => ProjectFileStatus.Alpha
        };

        var file = dto.Files?.FirstOrDefault();
        var fileName = file?.FileName ?? string.Empty;
        var url = file?.Url ?? string.Empty; // TODO: impl mirror
        var hash = file?.Hashes
            .First(hash => hash.Key.Equals("sha1", StringComparison.OrdinalIgnoreCase))
            .Value ?? string.Empty;

        var (type, modLoaders) = ModLoaderDetector.DetectModrinthType(defaultType, dto.Loaders);


        var rawDeps = new List<string>();
        var rawOptionalDeps = new List<string>();

        if (dto.Dependencies is not null)
        {
            var tempDpes = dto.Dependencies
                .Where(dep => dep.ProjectId is not "P7dR8mSH" and not "qvIfYCYJ" and not "")
                .GroupBy(keySelector: dep => dep.Type, elementSelector: dep => dep.ProjectId)
                .ToDictionary(keySelector: dep => dep.Key, elementSelector: dep => dep.ToList());

            if (tempDpes.TryGetValue("required", out var requires))
            {
                rawDeps = requires;
            }

            if (tempDpes.TryGetValue("optional", out var optionals))
            {
                rawOptionalDeps = optionals;
            }
        }

        var gameVers = dto.GameVersions
            .Select(ver => ver.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(ver => !string.IsNullOrEmpty(ver) && (ver.StartsWith("1.") || ver.StartsWith("b1.")))
            .Select(ver => ver switch
            {
                _ when ver.Contains('-') => $"{ver.Split('-')[0]} 预览版",
                _ when ver.StartsWith("b1.") => "远古版本",
                _ => ver
            })
            .Distinct().ToList();

        var sortedGameVers = Utils.GameVersionSorterUtil.SortGameVersions(gameVers);

        var info = new ProjectFileInfo
        {
            Id = dto.Id,
            ProjectId = dto.ProjectId,
            DisplayName = displayName,
            Status = status,
            FileName = fileName,
            DownloadUrls = [url],
            DownloadCount = dto.DownloadCount,
            Hash = hash,
            ModLoaders = modLoaders,
            RawDependencies = rawDeps,
            Dependencies = [], // NOTE: lm doesnt impl this, so we leave it blank
            RawOptionalDependencies = rawOptionalDeps,
            OptionalDependencies = [], // NOTE: lm doesnt impl this, so we leave it blank
            GameVersions = sortedGameVers,
            Type = type
        };

        return info;
    }
}