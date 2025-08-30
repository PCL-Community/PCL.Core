using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.CurseForge.Models;

public record CurseForgeLinks
{
    /// <summary>
    /// 该 Mod 在 CurseForge Website 上的 Url;
    /// </summary>
    [JsonPropertyName("websiteUrl")] public required string WebsiteUrl;
    /// <summary>
    /// 该 Mod 的 Wiki 页面
    /// </summary>
    [JsonPropertyName("wikiUrl")] public required string WikiUrl;
    /// <summary>
    /// 反馈 Bug 的 Url
    /// </summary>
    [JsonPropertyName("issueUrl")] public required string IssueUrl;
    /// <summary>
    /// 源代码 Url
    /// </summary>
    [JsonPropertyName("sourceUrl")] public required string SourceUrl;
};