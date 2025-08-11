using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Model;

public record ModrinthProject
{
    [JsonPropertyName("slug")] public required string Slug;
    [JsonPropertyName("title")] public required string Title;
    [JsonPropertyName("description")] public required string Description;
    [JsonPropertyName("categories")] public required List<string?> Category;
    [JsonPropertyName("client_site")] public required string ClientSite;
    [JsonPropertyName("server_site")] public required string ServerSite;
    [JsonPropertyName("project_type")] public required string ProjectType;
    [JsonPropertyName("downloads")] public int DownloadCount;
    [JsonPropertyName("icon_url")] public required string IconUrl;
    [JsonPropertyName("color")] public required string Color;
    [JsonPropertyName("thread_id")] public required string ThreadId;

    [JsonPropertyName("monetization_status")]
    public required string MonetizationStatus;

    [JsonPropertyName("project_id")] public required string ProjectId;
    [JsonPropertyName("author")] public required string Author;

    [JsonPropertyName("display_categories")]
    public required List<string?> DisplayCategory;

    [JsonPropertyName("versions")] public required List<string> Versions;
    [JsonPropertyName("follows")] public int Follows;
    [JsonPropertyName("date_created")] public required string DateCreated;
    [JsonPropertyName("date_modified")] public required string DateModified;
    [JsonPropertyName("latest_version")] public string? LatestVersion;
    [JsonPropertyName("license")] public required string License;
    [JsonPropertyName("gallery")] public required string Gallery;
    [JsonPropertyName("featured_galler")] public string? FeaturedGaller;
}