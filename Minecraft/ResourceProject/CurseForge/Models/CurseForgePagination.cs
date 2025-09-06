using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgePagination
{
    [JsonPropertyName("index")] public int Index;
    [JsonPropertyName("pageSize")] public int PageSize;
    [JsonPropertyName("resultCount")] public int ResultCount;
    [JsonPropertyName("totalCount")] public long TotalCount;
}