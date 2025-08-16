using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeProject
{
    [JsonPropertyName("id")] public int Id;
    [JsonPropertyName("gameId")] public int GameId;
    [JsonPropertyName("name")] public required string Name;
    [JsonPropertyName("slug")] public required string Slug;
    [JsonPropertyName("links")] public required CurseForgeLinks Links;
    [JsonPropertyName("summary")] public required string Summary;
    [JsonPropertyName("status")] public required int Status;
    [JsonPropertyName("downloadCount")] public long DownloadCount;
    [JsonPropertyName("isFeatured")] public bool IsFeatured;

    [JsonPropertyName("primaryCategoryId")]
    public int PrimaryCategoryId;

    [JsonPropertyName("categories")] public required List<CurseForgeCategory?> Categories;
    
    [JsonPropertyName("classId")] public int? ClassId;
    [JsonPropertyName("authors")] public required List<CurseForgeAuthor> Authors;
    [JsonPropertyName("logo")] public required CurseForgeAsset Logo;
    [JsonPropertyName("screenshots")] public required List<CurseForgeAsset?> ScreenShots;
    [JsonPropertyName("mainFileId")] public int MainFileId;
    [JsonPropertyName("latestFiles")] public required List<CurseForgeFile?> LatestFiles;

    [JsonPropertyName("latestFilesIndexes")]
    public required List<CurseForgeFileIndex> LatestFileIndexes;

    [JsonPropertyName("latestEarlyAccessFilesIndexes")]
    public required List<CurseForgeFileIndex> LatestEarlyAccessFilesIndexes;

    [JsonPropertyName("dataCreated")] public required string DateCreated;
    [JsonPropertyName("dateModified")] public required string DateModified;
    [JsonPropertyName("dateReleased")] public required string DataReleased;

    [JsonPropertyName("allowModDistribution")]
    public bool AllowModDistribution;

    [JsonPropertyName("gamePopularityRank")]
    public int GamePopularityRank;

    [JsonPropertyName("isAvailable")] public bool IsAvailable;

    [JsonPropertyName("thumbsUpCount")]
    public int ThumbsUoCount;

    [JsonPropertyName("rating")] public decimal? Rating;

}