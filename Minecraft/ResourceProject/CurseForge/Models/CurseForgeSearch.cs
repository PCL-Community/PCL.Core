using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeSearch
{
    [JsonPropertyName("data")] public required List<CurseForgeProject?> Data;
    [JsonPropertyName("pagination")] public required CurseForgePagination Pagination;
};