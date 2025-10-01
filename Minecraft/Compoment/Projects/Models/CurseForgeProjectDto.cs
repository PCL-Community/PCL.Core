using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.Compoment.Projects.Models;

public record CurseForgeProjectDto
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("slug")] public required string Slug { get; set; }

    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("summary")] public required string Summary { get; set; }

    [JsonPropertyName("links")] public required CurseForgeLinksDto Links { get; set; }

    [JsonPropertyName("dateReleased")] public DateTime DateReleased { get; set; }

    [JsonPropertyName("downloadCount")] public int DownloadCount { get; set; }

    [JsonPropertyName("logo")] public CurseForgeLogoDto? Logo { get; set; }

    [JsonPropertyName("latestFiles")] public List<CurseForgeFileDto> LatestFiles { get; set; } = [];

    [JsonPropertyName("latestFilesIndexes")]
    public List<CurseForgeFileIndexDto> LatestFilesIndexes { get; set; } = [];

    [JsonPropertyName("categories")] public List<CurseForgeCategoryDto> Categories { get; set; } = [];
}

public class CurseForgeLinksDto
{
    [JsonPropertyName("websiteUrl")] public required string WebsiteUrl { get; set; }
}

public class CurseForgeLogoDto
{
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
}

public class CurseForgeFileDto
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("gameVersions")] public List<string> GameVersions { get; set; } = []; /*...其他文件属性...*/
}

public class CurseForgeFileIndexDto
{
    [JsonPropertyName("fileId")] public int FileId { get; set; }
    [JsonPropertyName("gameVersion")] public required string GameVersion { get; set; }
}

public class CurseForgeCategoryDto
{
    [JsonPropertyName("id")] public int Id { get; set; }
}
