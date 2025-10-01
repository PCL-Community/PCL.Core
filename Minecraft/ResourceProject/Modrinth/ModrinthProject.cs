using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject.Modrinth.Models;

[Serializable]
public record ModrinthProject(
    string Slug,
    string Title,
    string Description,
    List<string> Categories,
    string ClientSide,
    string ServerSide,
    string Body,
    string Status,
    string? RequestedStatus,
    List<string> AdditionalCategories,
    string? IssuesUrl,
    string? SourceUrl,
    string? WikiUrl,
    string? DiscordUrl,
    List<ModrinthDonationUrl> DonationUrls,
    string ProjectType,
    int Downloads,
    string IconUrl,
    int Color,
    string ThreadId,
    string MonetizationStatus,
    string Id,
    string Team,
    string BodyUrl,
    ModrinthModeratorMessage ModeratorMessage,
    string Published,
    string Updated,
    string? Approved,
    string? Queued,
    int Followers,
    ModrinthLicense License,
    List<string> Versions,
    List<string> GameVersions,
    List<string> Loaders,
    List<object> Gallery);